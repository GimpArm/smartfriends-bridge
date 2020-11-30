using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class DeviceType
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("model")]
        public string Model { get; set; }

        [JsonProperty("switchingValues")]
        public SwitchingValue[] SwitchingValues { get; set; }

        [JsonProperty("max")]
        public int? Max { get; set; }

        [JsonProperty("min")]
        public int? Min { get; set; }

        [JsonProperty("precision")]
        public int? Precision { get; set; }

        [JsonProperty("step")]
        public int? Step { get; set; }
    }
}
