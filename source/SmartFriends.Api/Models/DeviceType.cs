using Newtonsoft.Json;
using SmartFriends.Api.JsonConvertes;

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
        //[JsonConverter(typeof(SwitchingValueConverter))]
        public SwitchingValue[] SwitchingValues { get; set; }

        [JsonProperty("TextOptions")]
        [JsonConverter(typeof(TextOptionArrayConverter))]
        public TextOption[] TextOptions { get; set; }

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
