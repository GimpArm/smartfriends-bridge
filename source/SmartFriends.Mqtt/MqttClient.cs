using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Formatter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SmartFriends.Api.JsonConvertes;
using SmartFriends.Api.Models;
using SmartFriends.Mqtt.Models;
using System.Globalization;
using System.Text;

namespace SmartFriends.Mqtt;

public class MqttClient : IDisposable
{
    public const string DeviceMapFile = "deviceMap.json";

    private static readonly JsonSerializerSettings SerializerSettings = new()
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
    private readonly MqttClientFactory _mqttFactory = null!;
    private readonly DeviceMap[] _deviceMap = [];
    private IMqttClient? _client;
    private CancellationTokenSource? _tokenSource;
    private Thread? _keepAliveThread;

    public Func<int, string?, string, Task>? RelayAction { get; set; }

    public MqttClient(IOptions<MqttConfiguration> mqttConfig, ILogger<MqttClient> logger, TypeTemplateEngine templateEngine)
    {
        _logger = logger;
        _mqttConfig = mqttConfig.Value;
        _typeTemplateEngine = templateEngine;
        if (!_mqttConfig.Enabled) return;

        if (!string.IsNullOrWhiteSpace(_mqttConfig.DataPath))
        {
            Directory.CreateDirectory(_mqttConfig.DataPath);
        }
        _deviceMap = LoadDeviceMap(Path.Combine(_mqttConfig.DataPath, DeviceMapFile));
        _logger.LogInformation($"Loaded {_deviceMap.Length} devices to map.");
        _mqttFactory = new MqttClientFactory();
    }

    private DeviceMap[] LoadDeviceMap(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                return JsonConvert.DeserializeObject<DeviceMap[]>(File.ReadAllText(path)) ?? [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Invalid deviceMap.json file. {ex.Message}");
                return Array.Empty<DeviceMap>();
            }
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

        if (_tokenSource != null)
        {
            await _tokenSource.CancelAsync();
        }
        _tokenSource = new CancellationTokenSource();

        if (!await TryConnectAsync(_tokenSource.Token)) return false;

        _keepAliveThread = new Thread(Keepalive);
        _keepAliveThread.Start(_tokenSource.Token);

        return true;
    }

    private async Task<bool> TryConnectAsync(CancellationToken token)
    {
        try
        {
            _client = _mqttFactory.CreateMqttClient();
            _client.ConnectedAsync += ConnectedHandler;
            _client.DisconnectedAsync += DisconnectedHandler;
            _client.ApplicationMessageReceivedAsync += MessageReceived;

            await _client.ConnectAsync(ClientOptions().Build(), token);

            var subscriptionOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter($"{_mqttConfig.BaseTopic}/#")
                .WithTopicFilter("home-assistant/#")
                .Build();

            await _client.SubscribeAsync(subscriptionOptions, token);

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
        var protocolVersion = !string.IsNullOrWhiteSpace(_mqttConfig.ProtocolVersion) && Enum.TryParse<MqttProtocolVersion>(_mqttConfig.ProtocolVersion, out var result) ? result : MqttProtocolVersion.V500;

        var options = new MqttClientOptionsBuilder()
            .WithClientId(Guid.NewGuid().ToString())
            .WithTcpServer(_mqttConfig.Server, _mqttConfig.Port)
            .WithKeepAlivePeriod(TimeSpan.FromSeconds(60))
            .WithProtocolVersion(protocolVersion)
            .WithTlsOptions(x =>
            {
                x.UseTls(_mqttConfig.UseSsl)
                    .WithIgnoreCertificateChainErrors()
                    .WithIgnoreCertificateRevocationErrors()
                    .WithAllowUntrustedCertificates();
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
            await UpdateStatus(false);
            await _client.DisconnectAsync(new MqttClientDisconnectOptions(), CancellationToken.None);
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

            _logger.LogInformation($"Sending device information for {deviceId} '{name}'");

            var payload = JObject.FromObject(new DeviceRegistration
            {
                CommandTopic = $"{_mqttConfig.BaseTopic}/{deviceId}/set",
                JsonAttributesTopic = $"{_mqttConfig.BaseTopic}/{deviceId}",
                StateTopic = $"{_mqttConfig.BaseTopic}/{deviceId}/state",
                Availability = new Availability
                {
                    Topic = $"{_mqttConfig.BaseTopic}/bridge/state"
                },
                Device = new Device
                {
                    Identifiers = [deviceId],
                    Manufacturer = device.Manufacturer,
                    Model = device.Model ?? device.Kind,
                    Name = name,
                    ViaDevice = device.GatewayDevice
                },
                DeviceClass = map.Class,
                UniqueId = $"{deviceId}_{map.Type}_{_mqttConfig.BaseTopic}"
            }, Serializer);

            _typeTemplateEngine.Merge(payload, map, deviceId);

            await _client!.PublishAsync(
                new MqttApplicationMessage
                {
                    Topic = $"homeassistant/{map.Type}/{deviceId}/{map.Class ?? map.Type}/config",
                    ContentType = "application/json",
                    Retain = true,
                    PayloadSegment = Encoding.UTF8.GetBytes(payload.ToString())
                });
            await _client.PublishAsync(
                new MqttApplicationMessage
                {
                    Topic = $"{_mqttConfig.BaseTopic}/{deviceId}",
                    ContentType = "application/json",
                    Retain = true,
                    PayloadSegment = MakePayload(device)
                }
            );
        }

        await UpdateStatus(true);
    }

    public async Task DeviceUpdated(DeviceMaster deviceInfo, DeviceValue value)
    {
        var device = _deviceMap.FirstOrDefault(x => x.Id == value.MasterDeviceId);
        if (device == null) return;

        _logger.LogInformation($"Device update {IdPrefix}{device.Id}");

        await _client!.PublishAsync(new MqttApplicationMessage
        {
            Topic = $"{_mqttConfig.BaseTopic}/{IdPrefix}{device.Id}/state",
            ContentType = "text/plain",
            Retain = true,
            PayloadSegment = MakePayload(deviceInfo.State)
        });
        foreach (var message in deviceInfo.Devices.Where(x => x.Value.CurrentValue != null).Select(x => new MqttApplicationMessage
        {
            Topic = $"{_mqttConfig.BaseTopic}/{IdPrefix}{device.Id}/{x.Key}",
            ContentType = "text/plain",
            Retain = true,
            PayloadSegment = MakePayload(x.Value.CurrentValue)
        }))
        {
            await _client.PublishAsync(message);
        }
    }

    private Task ConnectedHandler(MqttClientConnectedEventArgs e)
    {
        _logger.LogInformation($"Client Connected to mqtt{(_mqttConfig.UseSsl ? "s" : string.Empty)}://{_mqttConfig.Server}:{_mqttConfig.Port}");
        return Task.CompletedTask;
    }

    private Task DisconnectedHandler(MqttClientDisconnectedEventArgs e)
    {
        _logger.LogInformation($"Client Disconnected {e.Reason}");
        return Task.CompletedTask;
    }

    private async Task MessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        if (!e.ApplicationMessage.Topic.StartsWith($"{_mqttConfig.BaseTopic}/{IdPrefix}")) return;

        var topicParts = e.ApplicationMessage.Topic.Split("/");
        if (topicParts.Length < 3 || !topicParts.Last().Equals("set", StringComparison.InvariantCultureIgnoreCase)) return;

        var payload = !e.ApplicationMessage.Payload.IsEmpty ? Encoding.UTF8.GetString(e.ApplicationMessage.Payload) : string.Empty;

        _logger.LogInformation($"Received '{e.ApplicationMessage.Topic}': {payload}");

        if (!int.TryParse(topicParts[1].Substring(3), out var deviceId)) return;

        var kind = topicParts.Length > 3 ? topicParts[2] : null;

        if (RelayAction != null)
        {
            await RelayAction.Invoke(deviceId, kind, payload);
        }
    }

    private async Task UpdateStatus(bool online)
    {
        await _client!.PublishAsync(new MqttApplicationMessage
        {
            Topic = $"{_mqttConfig.BaseTopic}/bridge/state",
            Retain = true,
            PayloadSegment = Encoding.UTF8.GetBytes(online ? "online" : "offline")
        });
    }

    private void Keepalive(object? input)
    {
        try
        {
            if (input is not CancellationToken token) throw new Exception("Thread sent null cancellation token");

            while (!token.IsCancellationRequested)
            {
                if (_client?.IsConnected ?? false)
                {
                    UpdateStatus(true).GetAwaiter().GetResult();

                    //3 minutes
                    Task.Delay(180000, token).Wait(token);
                }
                else
                {
                    //Mimic the managed client's auto reconnect delay
                    Task.Delay(5000, token).Wait(token);
                    if (!token.IsCancellationRequested)
                    {
                        TryConnectAsync(token).GetAwaiter().GetResult();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            //Eat the operation cancelled exceptions
        }
    }

    private byte[] MakePayload(object? input)
    {
        if (input is FuzzyValue fuzzy && !fuzzy.ShouldSerialize)
        {
            return Encoding.UTF8.GetBytes(string.Format(CultureInfo.InvariantCulture, "{0}", fuzzy.Value));
        }
        return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(input, SerializerSettings));
    }

    public void Dispose()
    {
        Close().GetAwaiter().GetResult();
    }
}