using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartFriends.Api.Helpers;
using SmartFriends.Api.Interfaces;
using SmartFriends.Api.JsonConvertes;
using SmartFriends.Api.Models;
using SmartFriends.Api.Models.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartFriends.Api
{
    public class Client : IDisposable, IClient
    {
        private readonly SemaphoreSlim _commandSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);
        private readonly Configuration _configuration;
        private readonly ILogger _logger;
        private readonly ConcurrentQueue<Message> _messageQueue = new ConcurrentQueue<Message>();

        private TcpClient _client;
        private SslStream _stream;
        private GatewayInfo _deviceInfo;
        private Thread _readerThread;
        private CancellationTokenSource _tokenSource;

        public bool Connected { get; private set; }

        public string GatewayDevice => _deviceInfo?.Hardware;

        public event EventHandler<DeviceValue> DeviceUpdated;

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
                var cert = X509Certificate.CreateFromCertFile(Path.Combine(new FileInfo(GetType().Assembly.Location).DirectoryName, "CA.pem"));
                _client = new TcpClient(_configuration.Host, _configuration.Port);
                _stream = new SslStream(_client.GetStream(), false, ValidateServerCertificate, null);
                await _stream.AuthenticateAsClientAsync(_configuration.Host, new X509CertificateCollection(new[] { cert }), SslProtocols.Tls, false);
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

            _tokenSource?.Cancel();
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

        public async Task<T> SendAndReceiveCommand<T>(CommandBase command, int timeout = 2500)
        {
            var message = await SendCommand(command, false, timeout);
            if (typeof(T) == typeof(Message))
            {
                return (T)(object)message;
            }
            return message == null ? default : message.Response.ToObject<T>();
        }

        public async Task<bool> SendCommand(CommandBase command, int timeout = 2500)
        {
            var message = await SendCommand(command, false);
            return message?.ResponseMessage?.Equals("success", StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        private async Task<Message> SendCommand(CommandBase command, bool skipEnsure, int timeout = 2500)
        {
            if (!skipEnsure)
            {
                await EnsureConnection();
            }
            Message message;
            command.SessionId = _deviceInfo?.SessionId;
            var json = Serialize(command);
            _logger.LogDebug($"Send: {json}");
            await _commandSemaphore.WaitAsync();
            using var token = new CancellationTokenSource(timeout);
            try
            {
                //Clear the queue so we don't get old messages.
                while (_messageQueue.TryDequeue(out message))
                {
                    _logger.LogInformation($"Abandoned message {JsonConvert.SerializeObject(message)}");
                }

                await _stream.WriteAsync(Encoding.UTF8.GetBytes(json), token.Token);
                while (!_messageQueue.TryDequeue(out message) && !token.IsCancellationRequested)
                {
                    await Task.Delay(10, token.Token);
                }
            }
            finally
            {
                _commandSemaphore.Release();
            }
            return message;
        }

        private async Task StartSession()
        {
            _logger.LogInformation($"Logging in as {_configuration.Username}");
            var info = await SendCommand(new Hello(_configuration.Username), true);
            var digest = LoginHelper.CalculateDigest(_configuration.Password, info.Response.ToObject<SaltInfo>());
            var message = await SendCommand(new Login(_configuration.Username, digest, _configuration.CSymbol + _configuration.CSymbolAddon, _configuration.ShcVersion, _configuration.ShApiVersion), true);
            _deviceInfo = message.Response.ToObject<GatewayInfo>();
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

        private void Reader(object input)
        {
            try
            {
                var token = (CancellationToken)input;
                while (!token.IsCancellationRequested)
                {
                    var buffer = new byte[1024];
                    var messageData = new StringBuilder();
                    int bytes;
                    do
                    {
                        bytes = _stream.ReadAsync(buffer, 0, buffer.Length, token).Result;

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
                        // Smartfriends problably sent 2 messages packed inside 1.
                        _logger.LogInformation($"Received invalid json message, will try to look for multiple objects: {messageData}");
                        foreach (var message in ParseMultiMessage(messageData))
                        {
                            HandleMessage(message);
                        }
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

        private void HandleMessage(Message message)
        {
            if (message == null) return;

            switch (message?.ResponseMessage)
            {
                case "newDeviceValue":
                    DeviceUpdated?.Invoke(this, message.Response.ToObject<DeviceValue>());
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
                    _messageQueue.Enqueue(message);
                    break;
            }
        }

        private IEnumerable<Message> ParseMultiMessage(StringBuilder input)
        {
            var openBrace = 0;
            var openSingleQuote = false;
            var openDoubleQuote = false;
            var messageData = new StringBuilder();
            for (var i = 0; i < input.Length; ++i)
            {
                switch (input[i])
                {
                    case '}':
                        if (!openSingleQuote && !openDoubleQuote)
                        {
                            openBrace--;
                            if (openBrace == 0)
                            {
                                messageData.Append(input[i]);
                                var message = Deserialize<Message>(messageData.ToString());
                                messageData.Clear();
                                if (message != null)
                                {
                                    yield return message;
                                }
                            }
                        }
                        break;
                    case '{':
                        if (!openSingleQuote && !openDoubleQuote)
                        {
                            openBrace++;
                        }
                        messageData.Append(input[i]);
                        break;
                    case '"':
                        if (!openSingleQuote)
                        {
                            openDoubleQuote = !openDoubleQuote;
                        }
                        messageData.Append(input[i]);
                        break;
                    case '\'':
                        if (!openDoubleQuote)
                        {
                            openSingleQuote = !openSingleQuote;
                        }
                        messageData.Append(input[i]);
                        break;
                }
            }
            if (messageData.Length > 5)
            {
                var message = Deserialize<Message>(messageData.ToString());
                if (message != null)
                {
                    yield return message;
                }
            }
        }

        private static string Serialize(object input)
        {
            return JsonConvert.SerializeObject(input) + "\n";
        }


        private T Deserialize<T>(string input)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(input, new JsonSerializerSettings
                {
                    Converters = new JsonConverter[] { new BooleanNumberConverter(), new TextOptionArrayConverter(), new HasHsvValueConverter(), new HsvValueConverter() }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to parse message");
                return default(T);
            }
        }
    }
}
