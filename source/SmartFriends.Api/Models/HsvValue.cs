using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class HsvValue
    {
        [JsonProperty("h")]
        public int H { get; set; }

        [JsonProperty("s")]
        public int S { get; set; }

        [JsonProperty("v")]
        public int V { get; set; }
    }
}
