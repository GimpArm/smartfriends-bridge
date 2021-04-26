using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SmartFriends.Api.JsonConvertes;
using SmartFriends.Api.Models;
using SmartFriends.Mqtt.Models;

namespace SmartFriends.Mqtt
{
    public class MqttClient: IDisposable
    {
        public const string DeviceMapFile = "deviceMap.json";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new List<JsonConverter> { new FuzzyValueConverter() }
        };
        private static readonly JsonSerializer Serializer = JsonSerializer.Create(SerializerSettings);
        private const string IdPrefix = "sf_";

        private readonly ILogger _logger;
        private readonly MqttConfiguration _mqttConfig;
        private readonly TypeTemplateEngine _typeTemplateEngine;
        private readonly MqttFactory _mqttFactory;
        private readonly DeviceMap[] _deviceMap;
        private IManagedMqttClient _client;
        private CancellationTokenSource _tokenSource;
        private Thread _keepAliveThread;

        public Func<int, string, string, Task> RelayAction { get; set; }

        public MqttClient(MqttConfiguration mqttConfig, ILogger logger, TypeTemplateEngine templateEngine)
        {
            _logger = logger;
            _mqttConfig = mqttConfig;
            _typeTemplateEngine = templateEngine;
            if (!_mqttConfig.Enabled) return;

            if (!string.IsNullOrWhiteSpace(mqttConfig.DataPath))
            {
                Directory.CreateDirectory(mqttConfig.DataPath);
            }
            _deviceMap = LoadDeviceMap(Path.Combine(_mqttConfig.DataPath, DeviceMapFile));
            _mqttFactory = new MqttFactory();
        }

        private static DeviceMap[] LoadDeviceMap(string path)
        {
            if (File.Exists(path))
            {
                return JsonConvert.DeserializeObject<DeviceMap[]>(File.ReadAllText(path));
            }
            File.WriteAllText(path, $"[{Environment.NewLine}{Environment.NewLine}]");
            return Array.Empty<DeviceMap>();
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

                _client = _mqttFactory.CreateManagedMqttClient();
                _client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(ConnectedHandler);
                _client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(DisconnectedHandler);
                _client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(MessageReceived);

                var options = new ManagedMqttClientOptionsBuilder()
                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                    .WithClientOptions(ClientOptions())
                    .Build();

                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic($"{_mqttConfig.BaseTopic}/#").Build());
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("home-assistant").Build());
                await _client.StartAsync(options);

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

        private MqttClientOptionsBuilder ClientOptions()
        {
            var options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(_mqttConfig.Server, _mqttConfig.Port)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
                .WithProtocolVersion(MqttProtocolVersion.V500)
                .WithTls(x =>
                {
                    x.UseTls = _mqttConfig.UseSsl;
                    x.IgnoreCertificateChainErrors = true;
                    x.IgnoreCertificateRevocationErrors = true;
                    x.AllowUntrustedCertificates = true;
                });
            if (!string.IsNullOrEmpty(_mqttConfig.User))
            {
                options = options.WithCredentials(_mqttConfig.User, _mqttConfig.Password);
            }
            return options;
        }

        public async Task Close()
        {
            _tokenSource?.Cancel();
            if (_client == null || !_client.IsConnected) return;
            try
            {
                await UpdateStatus(false, CancellationToken.None);
                await _client.StopAsync();
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
                var map = _deviceMap.FirstOrDefault(x => x.Id == device.Id);
                if (map == null) continue;

                var name = $"{device.Room} {device.Name}".Trim();
                var deviceId = $"{IdPrefix}{device.Id}";

                var payload = JObject.FromObject(new DeviceRegistration
                {
                    Name = name,
                    CommandTopic = $"{_mqttConfig.BaseTopic}/{deviceId}/set",
                    JsonAttributesTopic = $"{_mqttConfig.BaseTopic}/{deviceId}",
                    StateTopic = $"{_mqttConfig.BaseTopic}/{deviceId}/state",
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
                    UniqueId = $"{deviceId}_{map.Type}_{_mqttConfig.BaseTopic}"
                }, Serializer);

                _typeTemplateEngine.Merge(payload, map, deviceId);

                await _client.PublishAsync(
                    new MqttApplicationMessage
                    {
                        Topic = $"homeassistant/{map.Type}/{deviceId}/{map.Class ?? map.Type}/config",
                        ContentType = "application/json",
                        Retain = true,
                        Payload = Encoding.UTF8.GetBytes(payload.ToString())
                    },
                    new MqttApplicationMessage
                    {
                        Topic = $"{_mqttConfig.BaseTopic}/{deviceId}",
                        ContentType = "application/json",
                        Retain = true,
                        Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(device, SerializerSettings))
                    }
                );
            }

            await UpdateStatus(true, CancellationToken.None);
        }

        public async Task DeviceUpdated(DeviceMaster deviceInfo, DeviceValue value)
        {
            var device = _deviceMap.FirstOrDefault(x => x.Id == value.MasterDeviceId);
            if (device == null) return;

            await _client.PublishAsync(new MqttApplicationMessage
            {
                Topic = $"{_mqttConfig.BaseTopic}/{IdPrefix}{device.Id}/state",
                ContentType = "text/plain",
                Retain = true,
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(deviceInfo.State, SerializerSettings))
            });
            await _client.PublishAsync(deviceInfo.Devices.Where(x => x.Value.CurrentValue != null).Select(x => new MqttApplicationMessage
            {
                Topic = $"{_mqttConfig.BaseTopic}/{IdPrefix}{device.Id}/{x.Key}",
                ContentType = "text/plain",
                Retain = true,
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(x.Value.CurrentValue, SerializerSettings))
            }));
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
            if (topicParts.Length < 3 || !topicParts.Last().Equals("set", StringComparison.InvariantCultureIgnoreCase)) return;

            var payload = e.ApplicationMessage.Payload != null ? Encoding.UTF8.GetString(e.ApplicationMessage.Payload) : string.Empty;

            _logger.LogInformation($"Received '{e.ApplicationMessage.Topic}': {payload}");

            if (!int.TryParse(topicParts[1].Substring(3), out var deviceId)) return;

            var kind = topicParts.Length > 3 ? topicParts[2] : string.Empty;

            if (RelayAction != null)
            {
                await RelayAction.Invoke(deviceId, kind, payload);
            }
        }

        private async Task UpdateStatus(bool online, CancellationToken token)
        {
            await _client.PublishAsync(new MqttApplicationMessage
            {
                Topic = $"{_mqttConfig.BaseTopic}/bridge/state",
                Retain = true,
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