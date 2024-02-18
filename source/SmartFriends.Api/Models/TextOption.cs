using Newtonsoft.Json;
using SmartFriends.Api.JsonConvertes;

namespace SmartFriends.Api.Models
{
    public class TextOption
    {
        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("value")]
        [JsonConverter((typeof(BooleanNumberConverter)))]
        public long Value { get; set; }
    }
}
