using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SmartFriends.Api.Helpers;
using SmartFriends.Api.JsonConvertes;
using SmartFriends.Api.Models;
using SmartFriends.Api.Models.Commands;
using System;
using System.Collections.Concurrent;
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
    public class Client: IDisposable
    {
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
        private readonly Configuration _configuration;
        private readonly ILogger _logger;
        private readonly Thread _readerThread;
        private readonly ConcurrentQueue<Message> _messageQueue = new ConcurrentQueue<Message>();

        private TcpClient _client;
        private SslStream _stream;
        private GatewayInfo _deviceInfo;

        public bool Connected { get; private set; }

        public event EventHandler<DeviceValue> DeviceUpdated;

        public Client(Configuration configuration, ILogger logger)
        {
            _configuration = configuration;
            _logger = logger;
            _readerThread = new Thread(Reader);
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
            try
            {
                _logger.LogInformation($"Connecting to {_configuration.Host}");
                var cert = X509Certificate.CreateFromCertFile(Path.Combine(new FileInfo(GetType().Assembly.Location).DirectoryName, "CA.pem"));
                _client = new TcpClient(_configuration.Host, _configuration.Port);
                _stream = new SslStream(_client.GetStream(), false, ValidateServerCertificate, null);
                _stream.AuthenticateAsClient(_configuration.Host, new X509CertificateCollection(new[] { cert }), SslProtocols.Tls, false);
                _readerThread.Start();
                await StartSession();
                Connected = !string.IsNullOrEmpty(_deviceInfo?.SessionId);

                if (!Connected)
                {
                    _logger.LogError("Login failed!");
                }
                else
                {
                    _logger.LogInformation($"Logged in {_deviceInfo?.Hardware}");
                }

                return Connected;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to open socket to host");
                Connected = false;
                _stream?.Close();
                _stream?.Dispose();
                _client?.Close();
                _client?.Dispose();
                return false;
            }
        }

        private async Task EnsureConnection()
        {
            if (Connected) return;
            await Open();
        }

        public Task Close()
        {
            Connected = false;
            _stream.Close();
            _client.Close();
            _deviceInfo = null;
            return Task.CompletedTask;
        }

        public async Task<T> SendAndReceiveCommand<T>(CommandBase command)
        {
            var message = await SendCommand(command, false);
            return message == null ? default : message.Response.ToObject<T>();
        }

        public async Task<bool> SendCommand(CommandBase command)
        {
            var message = await SendCommand(command, false);
            return message?.ResponseMessage?.Equals("success", StringComparison.InvariantCultureIgnoreCase) ?? false;
        }

        private async Task<Message> SendCommand(CommandBase command, bool skipEnsure)
        {
            if (!skipEnsure)
            {
                await EnsureConnection();
            }
            Message message = null;
            command.SessionId = _deviceInfo?.SessionId;
            var json = Serialize(command);
            _logger.LogDebug($"Send: {json}");
            await _semaphoreSlim.WaitAsync();
            using var token = new CancellationTokenSource(2500);
            try
            {
                await _stream.WriteAsync(Encoding.UTF8.GetBytes(json), token.Token);
                while (!_messageQueue.TryDequeue(out message) && !token.IsCancellationRequested)
                {
                    await Task.Delay(10, token.Token);
                }
            }
            finally
            {
                _semaphoreSlim.Release();
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

        private void Reader()
        {
            try
            {
                while (_client.Connected)
                {
                    var buffer = new byte[2048];
                    var messageData = new StringBuilder();
                    int bytes;
                    do
                    {
                        bytes = _stream.Read(buffer, 0, buffer.Length);

                        var decoder = Encoding.UTF8.GetDecoder();
                        var chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                        decoder.GetChars(buffer, 0, bytes, chars, 0);
                        messageData.Append(chars);

                        if (chars.Last() == '\n')
                        {
                            break;
                        }
                    } while (bytes != 0);

                    var message = Deserialize<Message>(messageData.ToString());
                    if (message.ResponseMessage == "newDeviceValue")
                    {
                        _logger.LogInformation($"Received Status: {messageData}");
                        DeviceUpdated?.Invoke(this, message.Response.ToObject<DeviceValue>());
                    }
                    else
                    {
                        _messageQueue.Enqueue(message);
                    }
                }

            }
            catch (Exception e)
            {
                //Only log if still connected.
                if (_client.Connected)
                {
                    _logger.LogError(e, "Connection closed");
                }
            }
        }

        private static string Serialize(object input)
        {
            return JsonConvert.SerializeObject(input) + "\n";
        }

        private static T Deserialize<T>(string input)
        {
            return JsonConvert.DeserializeObject<T>(input, new JsonSerializerSettings
            {
                Converters = new JsonConverter[]{ new SwitchingValueConverter() }
            });
        }
    }
}
