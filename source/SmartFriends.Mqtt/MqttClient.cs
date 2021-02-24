using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SmartFriends.Api.Models;
using SmartFriends.Mqtt.Models;

namespace SmartFriends.Mqtt
{
    public class MqttClient: IDisposable
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        private static readonly JsonSerializer Serializer = JsonSerializer.Create(SerializerSettings);
        private const string IdPrefix = "sf_";

        private readonly ILogger _logger;
        private readonly MqttConfiguration _mqttConfig;
        private readonly TypeTemplateEngine _typeTemplateEngine;
        private readonly MqttFactory _mqttFactory;
        private IMqttClient _client;
        private CancellationTokenSource _tokenSource;
        private Thread _keepAliveThread;

        public Func<int, string, Task> RelayAction { get; set; }

        public MqttClient(MqttConfiguration mqttConfig, ILogger logger, TypeTemplateEngine templateEngine)
        {
            _logger = logger;
            _mqttConfig = mqttConfig;
            _typeTemplateEngine = templateEngine;
            _mqttFactory = new MqttFactory();
        }

        public async Task<bool> Open()
        {
            if (_client != null)
            {
                if (_client.IsConnected) return true;

                _client.Dispose();
                _client = null;
            }

            try
            {
                _tokenSource?.Cancel();
                _tokenSource = new CancellationTokenSource();

                _client = _mqttFactory.CreateMqttClient();
                _client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(ConnectedHandler);
                _client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(DisconnectedHandler);
                _client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(MessageReceived);

                var options = new MqttClientOptions
                {
                    ClientId = "ClientPublisher",
                    ProtocolVersion = MqttProtocolVersion.V311,
                    CleanSession = true,
                    KeepAlivePeriod = TimeSpan.FromSeconds(5),
                    ChannelOptions = new MqttClientTcpOptions
                    {
                        Server = _mqttConfig.Server,
                        Port = _mqttConfig.Port,
                        TlsOptions = new MqttClientTlsOptions
                        {
                            UseTls = _mqttConfig.UseSsl,
                            IgnoreCertificateChainErrors = true,
                            IgnoreCertificateRevocationErrors = true,
                            AllowUntrustedCertificates = true
                        }
                    },
                    Credentials = new MqttClientCredentials
                    {
                        Username = _mqttConfig.User,
                        Password = Encoding.UTF8.GetBytes(_mqttConfig.Password)
                    }
                };

                var connectResult = await _client.ConnectAsync(options, CancellationToken.None);
                if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
                {
                    throw new Exception($"Connection returned non-success code {connectResult.ResultCode}: {connectResult.ReasonString}");
                }

                await _client.SubscribeAsync(new MqttClientSubscribeOptions
                {
                    TopicFilters = new List<MqttTopicFilter> {new MqttTopicFilter {Topic = $"{_mqttConfig.BaseTopic}/#"}, new MqttTopicFilter {Topic = "home-assistant"}}
                }, CancellationToken.None);

                _keepAliveThread = new Thread(Keepalive);
                _keepAliveThread.Start(_tokenSource.Token);

                return true;
            }
            catch (Exception e)
            {
                _client?.Dispose();
                _client = null;
                _logger.LogError(e, $"Failed connection to mqtt{(_mqttConfig.UseSsl ? "s" : string.Empty)}://{_mqttConfig.Server}:{_mqttConfig.Port}");
                return false;
            }
        }

        public async Task Close()
        {
            _tokenSource?.Cancel();
            if (_client == null || !_client.IsConnected) return;
            try
            {
                await UpdateStatus(false, CancellationToken.None);

                await _client.DisconnectAsync(new MqttClientDisconnectOptions
                {
                    ReasonCode = MqttClientDisconnectReason.NormalDisconnection
                }, CancellationToken.None);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception while closing connection");
            }
            finally
            {
                _client?.Dispose();
                _client = null;
            }
        }

        public async Task SendDevices(IEnumerable<DeviceMaster> deviceMasters)
        {
            foreach (var device in deviceMasters)
            {
                var map = _mqttConfig.DeviceMaps?.FirstOrDefault(x => x.Id == device.Id);
                if (map == null) continue;

                var name = $"{device.Room} {device.Name}".Trim();
                var deviceId = $"{IdPrefix}{device.Id}";

                var payload = JObject.FromObject(new DeviceRegistration
                {
                    Name = name,
                    CommandTopic = $"{_mqttConfig.BaseTopic}/{deviceId}/set",
                    JsonAttributesTopic = $"{_mqttConfig.BaseTopic}/{deviceId}",
                    StateTopic = $"{_mqttConfig.BaseTopic}/{deviceId}",
                    Availability = new Availability
                    {
                        Topic = $"{_mqttConfig.BaseTopic}/bridge/state"
                    },
                    Device = new Device
                    {
                        Identifiers = new List<string> { deviceId },
                        Manufacturer = device.Manufacturer,
                        Model = device.Model ?? device.Kind,
                        Name = name,
                        ViaDevice = device.GatewayDevice
                    },
                    DeviceClass = map.Class,
                    UniqueId = $"{deviceId}_{map.Type}_{_mqttConfig.BaseTopic}",
                    ValueTemplate = "{{ value_json.controlValue }}"
                }, Serializer);

                _typeTemplateEngine.Merge(payload, map, deviceId);

                await _client.PublishAsync(
                    new MqttApplicationMessage
                    {
                        Topic = $"homeassistant/{map.Type}/{deviceId}/{map.Class ?? map.Type}/config",
                        ContentType = "application/json",
                        Payload = Encoding.UTF8.GetBytes(payload.ToString())
                    },
                    new MqttApplicationMessage
                    {
                        Topic = $"{_mqttConfig.BaseTopic}/{deviceId}",
                        ContentType = "application/json",
                        Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(device, SerializerSettings))
                    }
                );
            }

            await UpdateStatus(true, CancellationToken.None);
        }

        public async Task DeviceUpdated(DeviceMaster deviceInfo, DeviceValue value)
        {
            var device = _mqttConfig.DeviceMaps?.FirstOrDefault(x => x.Id == value.MasterDeviceID);
            if (device == null) return;

            await _client.PublishAsync(new MqttApplicationMessage
            {
                Topic = $"{_mqttConfig.BaseTopic}/{IdPrefix}{device.Id}",
                ContentType = "application/json",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceInfo, SerializerSettings))
            }, CancellationToken.None);
        }

        private void ConnectedHandler(MqttClientConnectedEventArgs e)
        {
            _logger.LogInformation($"Client Connected to mqtt{(_mqttConfig.UseSsl ? "s" : string.Empty)}://{_mqttConfig.Server}:{_mqttConfig.Port}");
        }

        private void DisconnectedHandler(MqttClientDisconnectedEventArgs e)
        {
            _logger.LogInformation("Client Disconnected");
        }

        private async Task MessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            if (!e.ApplicationMessage.Topic.StartsWith($"{_mqttConfig.BaseTopic}/{IdPrefix}")) return;

            var topicParts = e.ApplicationMessage.Topic.Split("/");
            if (topicParts.Length != 3) return;

            var payload = e.ApplicationMessage.Payload != null ? Encoding.UTF8.GetString(e.ApplicationMessage.Payload) : string.Empty;

            Console.WriteLine($"Received '{e.ApplicationMessage.Topic}': {payload}");

            if (!int.TryParse(topicParts[1].Substring(3), out var deviceId)) return;

            if (RelayAction != null)
            {
                await RelayAction.Invoke(deviceId, payload);
            }
        }

        private async Task UpdateStatus(bool online, CancellationToken token)
        {
            await _client.PublishAsync(new MqttApplicationMessage
            {
                Topic = $"{_mqttConfig.BaseTopic}/bridge/state",
                Payload = Encoding.UTF8.GetBytes(online ? "online" : "offline")
            }, token);
        }

        private void Keepalive(object input)
        {
            try
            {
                var token = (CancellationToken)input;
                while (!token.IsCancellationRequested && (_client?.IsConnected ?? false))
                {
                    UpdateStatus(true, token).GetAwaiter().GetResult();

                    //3 minutes
                    Task.Delay(180000, token).Wait(token);
                }
            }
            catch (OperationCanceledException)
            {
                //Eat the operation cancelled exceptions
            }
        }

        public void Dispose()
        {
            Close().GetAwaiter().GetResult();
        }
    }
}