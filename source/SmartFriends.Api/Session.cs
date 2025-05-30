﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartFriends.Api.Helpers;
using SmartFriends.Api.Interfaces;
using SmartFriends.Api.Models;
using SmartFriends.Api.Models.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartFriends.Api
{
    public class Session : IHostedService, IDisposable
    {
        private long _lastUpdate;
        private Thread _refreshThread;
        private CancellationTokenSource _tokenSource;
        private readonly IClient _client;
        private readonly ILogger _logger;

        private readonly List<DeviceDefinition> _definitions = new List<DeviceDefinition>();
        private readonly List<RoomInfo> _rooms = new List<RoomInfo>();

        public bool Ready { get; private set; }
        public readonly List<DeviceMaster> DeviceMasters = new List<DeviceMaster>();

        public JToken Raw { get; private set; }

        public event EventHandler<DeviceValue> DeviceUpdated;

        public Session(Configuration configuration, ILogger logger, IClient client = null)
        {
            _logger = logger;
            _client = client ?? new Client(configuration, logger);
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
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to open connection");
                return false;
            }
        }

        public DeviceMaster GetDevice(int id)
        {
            return DeviceMasters.FirstOrDefault(x => x.Id == id);
        }

        public async Task<bool> SetDeviceValue(int id, string kind, bool value)
        {
            var device = GetDevice(id);

            var command = device?.GetDigitalCommand(kind, value);
            if (command == null) return false;

            await _client.SendCommand(command);

            device.UpdateValue(kind, command.Value);
            return true;
        }

        public async Task<bool> SetDeviceValue(int id, string kind, int value)
        {
            var device = GetDevice(id);

            var command = device?.GetAnalogCommand(kind, value);
            if (command == null) return false;

            await _client.SendCommand(command);

            device.UpdateValue(kind, command.Value);
            return true;
        }

        public async Task<bool> SetDeviceValue(int id, string kind, string value)
        {
            var device = GetDevice(id);

            var command = device?.GetKeywordCommand(kind, value);
            if (command == null) return false;

            await _client.SendCommand(command);

            device.UpdateValue(kind, command.Value);

            return true;
        }

        public async Task<bool> SetDeviceValue(int id, string kind, HsvValue value)
        {
            var device = GetDevice(id);

            var command = device?.GetHsvCommand(kind, value);
            if (command == null) return false;

            await _client.SendCommand(command);

            device.UpdateValue(kind, command.Value);

            return true;
        }

        private void ClientDeviceUpdated(object sender, DeviceValue value)
        {
            var masterId = value.MasterDeviceId == 0 ? value.DeviceId : value.MasterDeviceId;
            var device = GetDevice(masterId);
            if (device?.SetValues(value) ?? false)
            {
                DeviceUpdated?.Invoke(this, value);
            }
        }

        public async Task RefreshDevices()
        {
            Message lastMessage = null;
            try
            {
                await _client.SendAndReceiveCommand<Message>(new GetAllNewInfos(_lastUpdate), message =>
                {
                    lastMessage = message;
                    _lastUpdate = message.Response?["currentTimestamp"]?.Value<long>() ?? throw new Exception("Server did not respond.");
                    if (!_definitions.Any())
                    {
                        var definitions = message.Response["newCompatibilityConfiguration"]?["compatibleRadioStandards"]?.ToObject<CompatibleDevices[]>()?.SelectMany(x => x.Definitions);
                        _definitions.AddRange(definitions?.Where(x => x.DeviceType != null) ?? Array.Empty<DeviceDefinition>());
                    }
                    _rooms.AddRange(message.Response["newRoomInfos"]?.ToObject<RoomInfo[]>()?.Where(x => x.RoomName != "${Service}") ?? Array.Empty<RoomInfo>());

                    if (message.Response["newDeviceInfos"].HasValues)
                    {
                        Raw = message.Response;
                    }
                    var deviceInfo = message.Response["newDeviceInfos"]?.ToObject<DeviceInfo[]>() ?? Array.Empty<DeviceInfo>();
                    Parallel.ForEach(deviceInfo, x =>
                    {
                        x.Definition = _definitions.FirstOrDefault(y => y.DeviceDesignation == x.DeviceDesignation);
                    });
                    foreach (var masterDevice in deviceInfo.GroupBy(x => x.MasterDeviceId == 0 ? x.DeviceId : x.MasterDeviceId))
                    {
                        var masterId = masterDevice.Key;
                        if (DeviceMasters.Any(x => x.Id == masterId)) continue;

                        var devices = masterDevice.ToArray();
                        var room = _rooms.FirstOrDefault(x => devices.Any(y => y.RoomId == x.RoomId));
                        if (room == null) continue;

                        var master = new DeviceMaster(masterId, room, devices)
                        {
                            GatewayDevice = _client.GatewayDevice
                        };
                        DeviceMasters.Add(master);
                    }
                    try
                    {
                        var values = message.Response["newDeviceValues"]?.ToObject<DeviceValue[]>() ?? Array.Empty<DeviceValue>();
                        foreach (var value in values)
                        {
                            ClientDeviceUpdated(this, value);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to deserialize device value\n{JsonConvert.SerializeObject(message.Response["newDeviceValues"])}");
                    }
                    Ready = true;
                }, 5000);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Refresh failed\n{lastMessage}");
            }
        }

        private void Keepalive(object input)
        {
            try
            {
                var token = (CancellationToken)input;
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
