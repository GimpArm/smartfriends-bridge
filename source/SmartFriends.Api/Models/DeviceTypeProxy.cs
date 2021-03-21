using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SmartFriends.Api.Helpers;
using SmartFriends.Api.JsonConvertes;

namespace SmartFriends.Api.Models
{
    public class DeviceTypeProxy
    {
        private readonly Dictionary<string, int> _valueKeys;

        public DeviceTypeProxy(DeviceInfo device)
        {
            Id = device.DeviceId;
            var typeInfo = device.Definition?.DeviceType;
            if (typeInfo == null) return;

            Commands = typeInfo.SwitchingValues != null ? new Dictionary<string, int>(typeInfo.SwitchingValues.ToDictionary(k => k.Name.RemoveLanguageLookup(), v => v.Value), StringComparer.OrdinalIgnoreCase) : null;
            _valueKeys = typeInfo.TextOptions != null ? new Dictionary<string, int>(typeInfo.TextOptions.ToDictionary(k => k.Name, v => v.Value), StringComparer.OrdinalIgnoreCase) : null;

            Max = typeInfo.Max;
            Min = typeInfo.Min;
            Precision = typeInfo.Precision;
            Step = typeInfo.Step;
        }

        [JsonProperty("Id")]
        public int Id { get; }

        [JsonProperty("commands")]
        public Dictionary<string, int> Commands { get; }

        [JsonProperty("max")]
        public int? Max { get; }

        [JsonProperty("min")]
        public int? Min { get; }

        [JsonProperty("precision")]
        public int? Precision { get; }

        [JsonProperty("step")]
        public int? Step { get; }

        [JsonProperty("currentValue")]
        [JsonConverter(typeof(FuzzyValueConverter))]
        public FuzzyValue CurrentValue { get; private set; }

        public void SetValue(FuzzyValue value)
        {
            if (value.Value is int intValue)
            {
                var lookup = Commands ?? _valueKeys;
                if (lookup != null && lookup.Any())
                {
                    var newValue = lookup.Where(x => x.Value == intValue).Select(x => x.Key).FirstOrDefault();
                    CurrentValue = newValue != null ? new FuzzyValue(newValue) : null;
                    return;
                }
            }

            CurrentValue = value;
        }
    }
}
