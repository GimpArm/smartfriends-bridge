using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartFriends.Api.Helpers;
using SmartFriends.Api.Interfaces;
using SmartFriends.Api.JsonConvertes;
using SmartFriends.Api.Models;
using SmartFriends.Api.Models.Commands;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace SmartFriends.Api;

public class Client : IClient
{
    private readonly SemaphoreSlim _commandSemaphore = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);
    private readonly Configuration _configuration;
    private readonly ILogger _logger;

    private TcpClient? _client;
    private SslStream? _stream;
    private GatewayInfo? _deviceInfo;
    private Thread? _readerThread;
    private CancellationTokenSource? _tokenSource;

    public bool Connected { get; private set; }

    public string? GatewayDevice => _deviceInfo?.Hardware;

    public event EventHandler<DeviceValue>? DeviceUpdated;
    public event EventHandler<Message>? MessageReceived;

    public Client(Configuration configuration, ILogger logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public void Dispose()
    {
        Connected = false;
        _stream?.Close();
        _stream?.Dispose();
        _client?.Close();
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<bool> Open()
    {
        if (Connected) return Connected;

        await _connectionSemaphore.WaitAsync();
        try
        {
            if (Connected) return Connected;
            _logger.LogInformation($"Connecting to {_configuration.Host}");
            var cert = X509CertificateLoader.LoadCertificateFromFile(Path.Combine(new FileInfo(GetType().Assembly.Location).DirectoryName ?? string.Empty, "CA.pem"));
            _client = new TcpClient(_configuration.Host, _configuration.Port);
            _stream = new SslStream(_client.GetStream(), false, ValidateServerCertificate, null);
            await _stream.AuthenticateAsClientAsync(_configuration.Host, new X509CertificateCollection(new X509Certificate[] { cert }), SslProtocols.Tls12, false);
            await EnsureReader(true);
            await StartSession();
            Connected = !string.IsNullOrEmpty(_deviceInfo?.SessionId);

            if (!Connected)
            {
                _logger.LogError("Login failed!");
            }
            else
            {
                _logger.LogInformation($"Logged in to {_deviceInfo?.Hardware}");
            }

            return Connected;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to open socket to host");
            await Close();
            return false;
        }
        finally
        {
            _connectionSemaphore.Release();
        }
    }

    private async Task EnsureConnection()
    {
        if (!Connected)
        {
            await Open();
        }

        await EnsureReader();
    }

    private async Task EnsureReader(bool forceRestart = false)
    {
        if (!forceRestart && (_readerThread?.IsAlive ?? false)) return;

        if (_tokenSource != null)
        {
            await _tokenSource.CancelAsync();
        }
        while (_readerThread?.IsAlive ?? false)
        {
            await Task.Delay(10);
        }

        _readerThread = new Thread(Reader);
        _tokenSource = new CancellationTokenSource();
        _readerThread.Start(_tokenSource.Token);
    }

    public Task Close()
    {
        Connected = false;
        _tokenSource?.Cancel();
        _stream?.Close();
        _stream?.Dispose();
        _client?.Close();
        _client?.Dispose();
        _deviceInfo = null;
        return Task.CompletedTask;
    }

    public Task SendAndReceiveCommand<T>(CommandBase command, Action<T?> action, int timeout = 2500)
    {
        return SendAndReceiveCommand<T>(command, a => { action.Invoke(a); return Task.CompletedTask; }, timeout);
    }

    public Task SendAndReceiveCommand<T>(CommandBase command, Func<T?, Task> action, int timeout = 2500)
    {
        var finished = false;
        EventHandler<Message>? handler = null;
        handler = (_, message) =>
        {
            if (!command.IsReponse(message)) return;

            MessageReceived -= handler;
            if (typeof(T) == typeof(Message))
            {
                action.Invoke((T)(object)message).GetAwaiter().OnCompleted(() => finished = true);
            }
            else
            {
                action.Invoke(message.Response.ToObject<T>()).GetAwaiter().OnCompleted(() => finished = true);
            }
        };
        MessageReceived += handler;
        _ = SendCommand(command).ConfigureAwait(false);

        //Wait until the whole operation completes
        return Task.Run(async () =>
        {
            try
            {
                using var token = new CancellationTokenSource(timeout);
                while (!finished && !token.IsCancellationRequested)
                {
                    await Task.Delay(10, token.Token);
                }
            }
            catch (TaskCanceledException) { }
        });
    }

    public async Task SendCommand(CommandBase command)
    {
        if (!command.SkipEnsure || _stream == null)
        {
            await EnsureConnection();
        }
        command.SessionId = _deviceInfo?.SessionId;
        var json = Serialize(command);
        _logger.LogDebug($"Send: {json}");
        await _commandSemaphore.WaitAsync();
        try
        {
            await _stream!.WriteAsync(Encoding.UTF8.GetBytes(json));
        }
        finally
        {
            _commandSemaphore.Release();
        }
    }

    private async Task StartSession()
    {
        _logger.LogInformation($"Logging in as {_configuration.Username}");

        await SendAndReceiveCommand<SaltInfo>(new Hello(_configuration.Username), async info =>
        {
            if (info == null)
            {
                _logger.LogInformation("Invalid Salt");
                return;
            }

            var digest = LoginHelper.CalculateDigest(_configuration.Password, info);
            await SendAndReceiveCommand<GatewayInfo>(new Login(_configuration.Username, digest, _configuration.CSymbol + _configuration.CSymbolAddon, _configuration.ShcVersion, _configuration.ShApiVersion),
                gateway =>
                {
                    _deviceInfo = gateway;
                }).ConfigureAwait(false);
        });
    }

    private static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors) => true;

    private void Reader(object? input)
    {
        try
        {
            if (input is not CancellationToken token) throw new Exception("Thead send null cancelation token");

            while (!token.IsCancellationRequested)
            {
                var buffer = new byte[1024];
                var messageData = new StringBuilder();
                int bytes;
                do
                {
                    bytes = _stream!.ReadAsync(buffer, 0, buffer.Length, token).Result;

                    var decoder = Encoding.UTF8.GetDecoder();
                    var chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                    decoder.GetChars(buffer, 0, bytes, chars, 0);
                    messageData.Append(chars);

                    if (chars.Last() == '\n')
                    {
                        break;
                    }
                } while (bytes != 0 && !token.IsCancellationRequested);

                try
                {
                    foreach (var subMessage in messageData.ToString().Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
                    {
                        var message = Deserialize<Message>(subMessage);
                        HandleMessage(message);
                    }
                }
                catch
                {
                    _logger.LogInformation($"Received invalid json message: {messageData}");
                }
            }
        }
        catch (Exception e)
        {
            //Only log if still connected.
            if (Connected)
            {
                _logger.LogError(e, "Connection closed");
                Close().ConfigureAwait(false);
            }
        }
    }

    private void HandleMessage(Message? message)
    {
        if (message == null || message.ResponseCode == 5) return;

        if (message.ResponseCode == -1)
        {
            _logger.LogError($"ErrorCode: {message.Response["errorCode"]}");
            return;
        }

        switch (message.ResponseMessage)
        {
            case "newDeviceValue":
                var deviceValue = message.Response.ToObject<DeviceValue>();
                if (deviceValue != null)
                {
                    DeviceUpdated?.Invoke(this, deviceValue);
                }
                break;
            case "newSwitchingSequenceInfo":
                _logger.LogInformation($"Received Switch Sequence Info: {message.Response["switchingSequenceID"]}, nextActivationTime: {message.Response["nextActivationTime"]}");
                break;
            case "newSwitchingSequenceStatus":
                _logger.LogInformation($"Received Switch Sequence: {message.Response["switchingSequenceID"]}, status: {message.Response["status"]}");
                break;
            case "newDeviceInfo":
                _logger.LogInformation($"Received new device: {message.Response}");
                break;
            default:
                MessageReceived?.Invoke(this, message);
                break;
        }
    }

    private static string Serialize(object input)
    {
        return JsonConvert.SerializeObject(input) + "\n";
    }

    private T? Deserialize<T>(string input)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(input, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Converters = [new BooleanNumberConverter(), new TextOptionArrayConverter(), new HasHsvValueConverter(), new HsvValueConverter()]
            }) ?? default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to parse message");
            return default;
        }
    }
}