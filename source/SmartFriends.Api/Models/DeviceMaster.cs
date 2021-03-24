using SmartFriends.Api.Models.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SmartFriends.Api.Helpers;
using SmartFriends.Api.JsonConvertes;

namespace SmartFriends.Api.Models
{
    public class DeviceMaster
    {
        private readonly DeviceInfo[] _devices;
        private readonly RoomInfo _room;
        private readonly DeviceInfo _defaultDevice;

        public int Id { get; }

        public string Name => _defaultDevice.MasterDeviceName;

        public string Room => _room?.GetCleanName() ?? "Unknown";

        public string GatewayDevice { get; set; }

        public string Kind => Devices.Keys.FirstOrDefault()?.FirstCharToUpper();
        public string Manufacturer => _defaultDevice.Manufacturer;
        public string Model => _defaultDevice.ProductDesignation;

        [JsonConverter(typeof(FuzzyValueConverter))]
        public FuzzyValue State
        {
            get
            {
                try
                {
                    return Devices[_defaultDevice.Definition.DeviceType.Kind].CurrentValue;
                }
                catch
                {
                    return Devices.Any() ? Devices.First().Value.CurrentValue : null;
                }
            }
        }

        public Dictionary<string, DeviceTypeProxy> Devices { get; }

        public DeviceMaster(int id, RoomInfo roomInfo, IEnumerable<DeviceInfo> devices)
        {
            Id = id;
            _devices = devices.ToArray();
            Devices = new Dictionary<string, DeviceTypeProxy>(StringComparer.OrdinalIgnoreCase);
            foreach (var device in _devices.Where(x => !string.IsNullOrEmpty(x.Definition?.DeviceType?.Kind) &&
                                                       (x.Definition?.Hidden == null || !x.Definition.Hidden.ContainsKey("deviceOverview") || !x.Definition.Hidden["deviceOverview"])))
            {
                var kind = device.Definition.DeviceType.Kind;
                if (Devices.ContainsKey(kind))
                {
                    kind = $"{device.Definition.DeviceType.Kind}_{device.DeviceId}";
                }

                Devices.Add(kind, new DeviceTypeProxy(device));
            }

            _room = roomInfo;
            _defaultDevice = _devices.FirstOrDefault(x =>
                                 !string.IsNullOrEmpty(x.Definition?.DeviceType?.Kind) &&
                                 x.Definition.DeviceType.SwitchingValues != null) ?? _devices.First();
        }

        public SetDeviceValue GetDigitalCommand(string kind, bool on)
        {
            return GetKeywordCommand(kind, on ? "on" : "off");
        }

        public SetDeviceValue GetAnalogCommand(string kind, int value)
        {
            kind ??= Kind;
            if (!Devices.ContainsKey(kind)) return null;

            return new SetDeviceValue(Devices[kind].Id, value);
        }

        public SetDeviceValue GetKeywordCommand(string kind, string keyword)
        {
            kind ??= Kind;
            if (!Devices.ContainsKey(kind) || Devices[kind].Commands == null || !Devices[kind].Commands.ContainsKey(keyword)) return null;

            var value = Devices[kind].Commands[keyword];

            return new SetDeviceValue(Devices[kind].Id, value);
        }

        public SetDeviceHsvValue GetHsvCommand(string kind, HsvValue value)
        {
            kind ??= Kind;
            if (!Devices.ContainsKey(kind)) return null;

            return new SetDeviceHsvValue(Devices[kind].Id, value);
        }

        public bool SetValues(params DeviceValue[] values)
        {
            if (!values?.Any() ?? true) return false;
            var updated = false;

            foreach (var value in values)
            {
                var device = Devices.Select(x => x.Value).FirstOrDefault(x => x.Id == value.DeviceId);
                if (device == null) continue;

                device.SetValue(value.Value);
                updated = true;
            }

            return updated;
        }

        public void UpdateValue(string kind, object value)
        {
            kind ??= Kind;
            if (!Devices.ContainsKey(kind)) return;

            Devices[kind].SetValue(new FuzzyValue(value));
        }
    }
}
