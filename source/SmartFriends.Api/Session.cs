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
using Newtonsoft.Json;
using SmartFriends.Api.Helpers;

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

        public bool Ready { get; private set; }
        public readonly List<DeviceMaster> DeviceMasters = new List<DeviceMaster>();

        public event EventHandler<DeviceValue> DeviceUpdated;

        public Session(Configuration configuration, ILogger logger)
        {
            _logger = logger;
            _client = new Client(configuration, logger);
            _client.DeviceUpdated += ClientDeviceUpdated;
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
            var hsv = ConvertHsv(value);
            if (hsv != null)
            {
                return await SetDeviceValue(id, hsv);
            }

            var device = GetDevice(id);

            var command = device?.GetKeywordCommand(value);
            if (command == null) return false;

            if (!await _client.SendCommand(command)) return false;

            device.UpdateValue(command.Value);

            return true;
        }

        public async Task<bool> SetDeviceValue(int id, HsvValue value)
        {
            var device = GetDevice(id);

            var command = device?.GetHsvCommand(value);
            if (command == null) return false;

            if (!await _client.SendCommand(command)) return false;

            device.UpdateValue(command.Value);

            return true;
        }

        private static HsvValue ConvertHsv(string value)
        {
            if (value == null || !value.StartsWith("{") || !value.EndsWith("}")) return null;
            try
            {
                return JsonConvert.DeserializeObject<HsvValue>(value);
            }
            catch
            {
                return null;
            }
        }

        private void ClientDeviceUpdated(object sender, DeviceValue value)
        {
            var device = GetDevice(value.MasterDeviceID);
            if (device?.SetValues(value) ?? false)
            {
                DeviceUpdated?.Invoke(this, value);
            }
        }

        public async Task RefreshDevices()
        {
            Message message = null;
            try
            {
                message = await _client.SendAndReceiveCommand<Message>(new GetAllNewInfos(_lastUpdate), 5000);
                var result = message.Response;
                _lastUpdate = result?["currentTimestamp"]?.Value<long>() ?? throw new Exception("Server did not respond.");
                if (!_definitions.Any())
                {
                    var definitions = result["newCompatibilityConfiguration"]?["compatibleRadioStandards"]?.ToObject<CompatibleDevices[]>()?.SelectMany(x => x.Definitions);
                    _definitions.AddRange(definitions?.Where(x => x.DeviceType != null) ?? Array.Empty<DeviceDefinition>());
                }
                _rooms.AddRange(result["newRoomInfos"]?.ToObject<RoomInfo[]>()?.Where(x => x.RoomName != "${Service}") ?? Array.Empty<RoomInfo>());

                var deviceInfo = result["newDeviceInfos"]?.ToObject<DeviceInfo[]>() ?? Array.Empty<DeviceInfo>();
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

                    var master = new DeviceMaster(masterId, room, devices)
                    {
                        GatewayDevice = _client.GatewayDevice
                    };
                    DeviceMasters.Add(master);
                }

                var values = result["newDeviceValues"]?.ToObject<DeviceValue[]>() ?? Array.Empty<DeviceValue>();
                foreach (var value in values)
                {
                    ClientDeviceUpdated(this, value);
                }

                Ready = true;
            }
            catch(Exception e)
            {
                _logger.LogError(e, $"Refresh failed\n{JsonConvert.SerializeObject(message)}");
            }
        }

        private void Keepalive(object input)
        {
            try
            {

                var token = (CancellationToken) input;
                while (!token.IsCancellationRequested)
                {
                    RefreshDevices().Wait(token);
                    Task.Delay(5000, token).Wait(token);
                }
            }
            catch (OperationCanceledException)
            {
                //Eat the operation cancelled exceptions
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
                while (!Ready && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(10, cancellationToken);
                }

                _logger.LogInformation($"===================Devices==================={Environment.NewLine}{DeviceMasters.Serialize()}");
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
