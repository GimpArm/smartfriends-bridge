using SmartFriends.Api.Models.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using SmartFriends.Api.Helpers;

namespace SmartFriends.Api.Models
{
    public class DeviceMaster
    {
        private readonly DeviceInfo[] _devices;
        private readonly RoomInfo _room;
        private readonly DeviceInfo _controlDevice;
        private readonly DeviceInfo _analogDevice;
        private readonly DeviceInfo _hsvDevice;
        private readonly DeviceInfo _colorTempDevice;

        public int Id { get; }

        public string Name { get; }

        public string Room => _room?.GetCleanName() ?? "Unknown";

        public string GatewayDevice { get; set; }

        public string Kind => _controlDevice?.Definition?.DeviceType?.Kind?.FirstCharToUpper();

        public string Manufacturer => _controlDevice?.Manufacturer;

        public string Model => _controlDevice?.ProductDesignation;

        public int ControlValue { get; private set; }

        public int? AnalogValue { get; private set; }

        public int? ColorTemp { get; private set; }

        public HsvValue Hsv { get; private set; }

        public string State => _controlDevice?.Definition?.DeviceType.SwitchingValues?.FirstOrDefault(x => x.Value == ControlValue)?.Name[2..^1];

        public string[] Commands => _controlDevice?.Definition?.DeviceType.SwitchingValues?.Select(x => x.Name[2..^1]).ToArray();

        public int? Min => _analogDevice?.Definition.DeviceType.Min;
        public int? Max => _analogDevice?.Definition.DeviceType.Max;
        public int? StepSize => _analogDevice?.Definition.DeviceType.Step;
        public int? ColorTempMin => _colorTempDevice?.Definition.DeviceType.Min;
        public int? ColorTempMax => _colorTempDevice?.Definition.DeviceType.Max;

        public DeviceMaster(int id, RoomInfo roomInfo, IEnumerable<DeviceInfo> devices)
        {
            Id = id;
            _devices = devices.ToArray();
            _room = roomInfo;
            _controlDevice = _devices.FirstOrDefault(x => x.Definition?.DeviceType?.SwitchingValues?.Any() ?? false);
            _colorTempDevice = _devices.FirstOrDefault(x => x.Definition?.DeviceType?.Kind.Equals("colortemp", StringComparison.InvariantCultureIgnoreCase) ?? false);
            _analogDevice = _devices.Where(x => x.DeviceId != (_colorTempDevice?.DeviceId ?? -1) && (x.Definition?.DeviceType?.Min.HasValue ?? false)).OrderByDescending(x => x.Definition.DeviceType.Max).ThenBy(x => x.Definition.DeviceType.Min).FirstOrDefault();
            _hsvDevice = _devices.FirstOrDefault(x => x.Definition?.DeviceType?.Kind.Equals("hsv", StringComparison.InvariantCultureIgnoreCase) ?? false);
            Name = _controlDevice?.MasterDeviceName;
        }

        public SetDeviceValue GetDigitalCommand(bool on)
        {
            return GetKeywordCommand(on ? "on" : "off");
        }

        public SetDeviceValue GetAnalogCommand(int value)
        {
            if (_colorTempDevice != null && ColorTempMax.HasValue && value <= ColorTempMax && ColorTempMin.HasValue && value >= ColorTempMin)
            {
                return new SetDeviceValue(_colorTempDevice.DeviceId, value);
            }

            if (_analogDevice == null)
            {
                return GetDigitalCommand(value > 0);
            }
            if (Max.HasValue && value > Max)
            {
                value = Max.Value;
            }
            if (Min.HasValue && value < Min)
            {
                value = Min.Value;
            }
            return new SetDeviceValue(_analogDevice.DeviceId, value);
        }

        public SetDeviceValue GetKeywordCommand(string keyword)
        {
            var name = $"${{{keyword}}}";
            var value = _controlDevice.Definition.DeviceType.SwitchingValues.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))?.Value;
            if (value.HasValue)
            {
                return new SetDeviceValue(_controlDevice.DeviceId, value.Value);
            }
            return null;
        }

        public SetDeviceHsvValue GetHsvCommand(HsvValue value)
        {
            return _hsvDevice != null ? new SetDeviceHsvValue(_hsvDevice.DeviceId, value) : null;
        }

        public bool SetValues(params DeviceValue[] values)
        {
            if (!values?.Any() ?? true) return false;
            var updated = false;

            if (_hsvDevice != null)
            {
                var hsv = values.FirstOrDefault(x => x.DeviceID == _hsvDevice.DeviceId);
                if (hsv?.Value.IsHsv ?? false)
                {
                    Hsv = (HsvValue) hsv.Value.Value;
                    ColorTemp = null;
                    updated = true;
                }
            }

            if (_controlDevice != null)
            {
                var control = values.FirstOrDefault(x => x.DeviceID == _controlDevice.DeviceId);
                if (control != null)
                {
                    //var newValue = control.Value is JObject jobj ? jobj["current"].Value<int>() : Convert.ToInt32(control.Value);
                    var newValue = Convert.ToInt32(control.Value.Value);
                    if (newValue != ControlValue)
                    {
                        ControlValue = newValue;
                        updated = true;
                    }
                }
            }

            if (_colorTempDevice != null)
            {
                var temp = values.FirstOrDefault(x => x.DeviceID == _colorTempDevice.DeviceId);
                if (temp != null)
                {
                    ColorTemp = Convert.ToInt32(temp.Value.Value);
                    Hsv = null;
                    updated = true;
                }
            }

            if (_analogDevice == null) return updated;

            var analog = values.FirstOrDefault(x => x.DeviceID == _analogDevice.DeviceId);
            if (analog != null)
            {
                //AnalogValue = analog.Value is JObject jobj ? jobj["current"].Value<int>() : Convert.ToInt32(analog.Value);
                AnalogValue = Convert.ToInt32(analog.Value.Value);
                return true;
            }

            return updated;
        }

        public void UpdateValue(HsvValue value)
        {
            Hsv = value;
            ColorTemp = null;
        }

        public void UpdateValue(int value)
        {
            //probably a color temp.
            if (_colorTempDevice != null && ColorTempMax.HasValue && value <= ColorTempMax && ColorTempMin.HasValue && value >= ColorTempMin)
            {
                ColorTemp = value;
                Hsv = null;
                return;
            }

            //probably a switching command
            if (_controlDevice.Definition.DeviceType.SwitchingValues.Any(x => x.Value == value))
            {
                if (value == 1 || value == 0)
                {
                    ControlValue = value;
                    return;
                }
                //probably a toggle but could be anything like a stop. It doesn't matter too much if we're wrong it will be refreshed.
                if (value == 2)
                {
                    ControlValue = ControlValue == 0 ? 1 : 0;
                    return;
                }
            }

            //Anything else just set the analog value if there is one.
            if (_analogDevice == null) return;

            AnalogValue = value;
        }
    }
}
