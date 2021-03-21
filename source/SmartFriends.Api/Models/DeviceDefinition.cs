using System.Collections.Generic;
using Newtonsoft.Json;
using SmartFriends.Api.JsonConvertes;

namespace SmartFriends.Api.Models
{
    public class DeviceDefinition
    {
        [JsonProperty("deviceDesignation")]
        public string DeviceDesignation { get; set; }

        [JsonProperty("deviceTypClient")]
        public string DeviceTypClient { get; set; }

        [JsonProperty("deviceTypServer")]
        public string DeviceTypServer { get; set; }

        [JsonProperty("deviceType")]
        public DeviceType DeviceType { get; set; }

        [JsonProperty("digitalValueOff")]
        [JsonConverter(typeof(SwitchingValueConverter))]
        public int? DigitalValueOff { get; set; }

        [JsonProperty("digitalValueOn")]
        [JsonConverter(typeof(SwitchingValueConverter))]
        public int? DigitalValueOn { get; set; }

        [JsonProperty("hidden")]
        public Dictionary<string, bool> Hidden { get; set; }
    }
}
