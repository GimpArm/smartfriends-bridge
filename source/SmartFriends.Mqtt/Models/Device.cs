using System.Collections.Generic;
using Newtonsoft.Json;

namespace SmartFriends.Mqtt.Models
{
    public class Device
    {
        [JsonProperty("connections")]
        public List<string> Connections { get; set; }

        [JsonProperty("identifiers")]
        public List<string> Identifiers { get; set; }

        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("sw_version")]
        public string Version { get; set; }

        [JsonProperty("via_device")]
        public string ViaDevice { get; set; }
    }
}
