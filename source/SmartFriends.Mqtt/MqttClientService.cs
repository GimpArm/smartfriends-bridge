using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartFriends.Api;
using SmartFriends.Api.JsonConvertes;
using SmartFriends.Api.Models;
using SmartFriends.Mqtt.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SmartFriends.Mqtt
{
    public class MqttClientService : IHostedService
    {
        private readonly ILogger<MqttClientService> _logger;
        private readonly Session _smartfriendsSession;
        private readonly MqttConfiguration _mqttConfig;
        private readonly MqttClient _mqttClient;

        public MqttClientService(ILogger<MqttClientService> logger, Session smartfriendsSession, IOptions<MqttConfiguration> mqttConfig, MqttClient mqttClient)
        {
            _logger = logger;
            _smartfriendsSession = smartfriendsSession;
            _mqttConfig = mqttConfig.Value;
            _mqttClient = mqttClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_mqttConfig.Enabled) return;

            _logger.LogInformation("MQTT StartAsync method called.");

            _mqttClient.RelayAction = ReplyActionRelay;
            while (!await _mqttClient.Open() && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }

            while (!_smartfriendsSession.Ready && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(10, cancellationToken);
            }

            if (!string.IsNullOrWhiteSpace(_mqttConfig.DataPath))
            {
                Directory.CreateDirectory(_mqttConfig.DataPath);
            }

            if (cancellationToken.IsCancellationRequested) return;

            await _mqttClient.SendDevices(_smartfriendsSession.DeviceMasters);

            _smartfriendsSession.DeviceUpdated += DeviceUpdatedRelay;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_mqttConfig.Enabled) return;
            _logger.LogInformation("MQTT StopAsync method called.");

            await _mqttClient.Close();
        }

        private async Task ReplyActionRelay(int deviceId, string kind, string payload)
        {
            if (int.TryParse(payload, out var intValue))
            {
                await _smartfriendsSession.SetDeviceValue(deviceId, kind, intValue);
                return;
            }
            if (bool.TryParse(payload, out var boolValue))
            {
                await _smartfriendsSession.SetDeviceValue(deviceId, kind, boolValue);
                return;
            }
            if (HsvValueConverter.TryParse(payload, out var hsvValue))
            {
                await _smartfriendsSession.SetDeviceValue(deviceId, kind, hsvValue);
                return;
            }

            await _smartfriendsSession.SetDeviceValue(deviceId, kind, payload);
        }

        private void DeviceUpdatedRelay(object sender, DeviceValue value)
        {
            _mqttClient.DeviceUpdated(_smartfriendsSession.GetDevice(value.MasterDeviceId), value).GetAwaiter().GetResult();
        }
    }
}
