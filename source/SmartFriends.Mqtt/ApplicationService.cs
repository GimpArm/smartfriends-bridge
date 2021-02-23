using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartFriends.Api;
using SmartFriends.Api.Models;

namespace SmartFriends.Mqtt
{
    public class ApplicationService: IHostedService
    {
        private readonly ILogger<ApplicationService> _logger;
        private readonly Session _smartfriendsSession;
        private readonly MqttClient _mqttClient;

        public ApplicationService(ILogger<ApplicationService> logger, Session smartfriendsSession, MqttClient mqttClient)
        {
            _logger = logger;
            _smartfriendsSession = smartfriendsSession;
            _mqttClient = mqttClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StartAsync method called.");

            await _smartfriendsSession.StartAsync(cancellationToken);

            _mqttClient.RelayAction = ReplyActionRelay;
            while (!await _mqttClient.Open() && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);
            }

            while (!_smartfriendsSession.Ready && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(10, cancellationToken);
            }

            if (cancellationToken.IsCancellationRequested) return;

            await _mqttClient.SendDevices(_smartfriendsSession.DeviceMasters);

            _smartfriendsSession.DeviceUpdated += DeviceUpdatedRelay;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("StopAsync method called.");

            await _smartfriendsSession.StopAsync(cancellationToken);
            await _mqttClient.Close();
        }

        private async Task ReplyActionRelay(int deviceId, string payload)
        {
            if (int.TryParse(payload, out var intValue))
            {
                await _smartfriendsSession.SetDeviceValue(deviceId, intValue);
            }
            if (bool.TryParse(payload, out var boolValue))
            {
                await _smartfriendsSession.SetDeviceValue(deviceId, boolValue);
            }

            await _smartfriendsSession.SetDeviceValue(deviceId, payload);
        }

        private void DeviceUpdatedRelay(object sender, DeviceValue value)
        {
            _mqttClient.DeviceUpdated(_smartfriendsSession.GetDevice(value.MasterDeviceID), value).GetAwaiter().GetResult();
        }
    }
}
