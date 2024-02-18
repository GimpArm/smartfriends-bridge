using Newtonsoft.Json;
using SmartFriends.Api.Helpers;
using SmartFriends.Api.JsonConvertes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartFriends.Api.Models
{
    public class DeviceTypeProxy
    {
        private readonly Dictionary<string, long> _valueKeys;

        public DeviceTypeProxy(DeviceInfo device)
        {
            Id = device.DeviceId;
            Description = device.DeviceDesignation?.RemoveLanguageLookup();

            var typeInfo = device.Definition?.DeviceType;
            if (typeInfo == null) return;

            Commands = typeInfo.SwitchingValues != null ? new Dictionary<string, long>(typeInfo.SwitchingValues.ToDictionary(k => k.Name.RemoveLanguageLookup(), v => v.Value), StringComparer.OrdinalIgnoreCase) : null;
            _valueKeys = typeInfo.TextOptions != null ? new Dictionary<string, long>(typeInfo.TextOptions.ToDictionary(k => k.Name, v => v.Value), StringComparer.OrdinalIgnoreCase) : null;

            Max = typeInfo.Max;
            Min = typeInfo.Min;
            Precision = typeInfo.Precision;
            Step = typeInfo.Step;
        }

        [JsonProperty("Id")]
        public int Id { get; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("commands")]
        public Dictionary<string, long> Commands { get; }

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
            if (value.Value is long intValue)
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
