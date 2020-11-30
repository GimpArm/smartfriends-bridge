using Newtonsoft.Json;
using SmartFriends.Api.JsonConvertes;

namespace SmartFriends.Api.Models
{
    public class SwitchingValue
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        [JsonConverter(typeof(SwitchingValueConverter))]
        public int Value { get; set; }
    }
}
