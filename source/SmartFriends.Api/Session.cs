using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using SmartFriends.Api.Models;
using SmartFriends.Api.Models.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartFriends.Api
{
    public class Session: IHostedService, IDisposable
    {
        private long _lastUpdate;
        private Thread _refreshThread;
        private CancellationTokenSource _tokenSource;
        private readonly Client _client;
        private readonly ILogger _logger;

        private readonly List<DeviceDefinition> _definitions = new List<DeviceDefinition>();
        private readonly List<RoomInfo> _rooms = new List<RoomInfo>();

        public readonly List<DeviceMaster> DeviceMasters = new List<DeviceMaster>();

        public Session(Configuration configuration, ILogger logger)
        {
            _logger = logger;
            _client = new Client(configuration, logger);
            _client.DeviceUpdated += DeviceUpdated;
        }

        public async Task<bool> Open()
        {
            try
            {
                var result = await _client.Open();
                if (!result) return false;
                _tokenSource?.Cancel();
                _tokenSource = new CancellationTokenSource();

                _refreshThread = new Thread(Keepalive);
                _refreshThread.Start(_tokenSource.Token);
                return true;
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Failed to open conneciton");
                return false;
            }
        }

        public DeviceMaster GetDevice(int id)
        {
            return DeviceMasters.FirstOrDefault(x => x.Id == id);
        }

        public async Task<bool> SetDeviceValue(int id, bool value)
        {
            var device = GetDevice(id);

            var command = device?.GetDigitalCommand(value);
            if (command == null) return false;

            if (!await _client.SendCommand(command)) return false;

            device.UpdateValue(command.Value);
            return true;
        }

        public async Task<bool> SetDeviceValue(int id, int value)
        {
            var device = GetDevice(id);

            var command = device?.GetAnalogCommand(value);
            if (command == null) return false;

            if (!await _client.SendCommand(command)) return false;

            device.UpdateValue(command.Value);
            return true;
        }

        public async Task<bool> SetDeviceValue(int id, string value)
        {
            var device = GetDevice(id);

            var command = device?.GetKeywordCommand(value);
            if (command == null) return false;

            if (!await _client.SendCommand(command)) return false;

            device.UpdateValue(command.Value);
            return true;
        }

        private void DeviceUpdated(object sender, DeviceValue value)
        {
            var device = GetDevice(value.MasterDeviceID);
            device?.SetValues(value);
        }

        public async Task RefreshDevices()
        {
            JObject result = null;
            try
            {
                result = await _client.SendAndReceiveCommand<JObject>(new GetAllNewInfos(_lastUpdate), 5000);
                _lastUpdate = result?["currentTimestamp"].Value<long>() ?? throw new Exception("Server did not respond.");
                if (!_definitions.Any())
                {
                    var definitions = result["newCompatibilityConfiguration"]["compatibleRadioStandards"].ToObject<CompatibleDevices[]>().SelectMany(x => x.Definitions);
                    _definitions.AddRange(definitions.Where(x => x.DeviceType != null));
                }
                _rooms.AddRange(result["newRoomInfos"].ToObject<RoomInfo[]>().Where(x => x.RoomName != "${Service}"));

                var deviceInfo = result["newDeviceInfos"].ToObject<DeviceInfo[]>();
                Parallel.ForEach(deviceInfo, x =>
                {
                    x.Definition = _definitions.FirstOrDefault(y => y.DeviceTypServer == x.DeviceTypServer && y.DeviceDesignation == x.DeviceDesignation);
                });
                foreach (var masterDevice in deviceInfo.GroupBy(x => x.MasterDeviceId))
                {
                    var masterId = masterDevice.Key;
                    if (DeviceMasters.Any(x => x.Id == masterId)) continue;

                    var devices = masterDevice.ToList();
                    var room = _rooms.FirstOrDefault(x => x.RoomId == devices[0].RoomId);
                    if (room == null) continue;

                    var master = new DeviceMaster(masterId, room, devices);
                    DeviceMasters.Add(master);
                }

                var values = result["newDeviceValues"].ToObject<DeviceValue[]>();
                foreach (var masterDevice in values.GroupBy(x => x.MasterDeviceID))
                {
                    var master = DeviceMasters.FirstOrDefault(x => x.Id == masterDevice.Key);

                    master?.SetValues(masterDevice.ToArray());
                }
            }
            catch(Exception e)
            {
                _logger.LogError(e, $"Refresh failed\n{result}");
            }
        }

        private void Keepalive(object input)
        {
            var token = (CancellationToken) input;
            while (!token.IsCancellationRequested)
            {
                RefreshDevices().Wait(token);
                Task.Delay(5000, token).Wait(token);
            }
        }

        public void Dispose()
        {
            _tokenSource?.Cancel();
            _client?.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            while (!await Open())
            {
                if (cancellationToken.IsCancellationRequested) break;

                await Task.Delay(5000, cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _tokenSource?.Cancel();
            if (_client != null)
            {
                await _client.Close();
            }
        }
    }
}
