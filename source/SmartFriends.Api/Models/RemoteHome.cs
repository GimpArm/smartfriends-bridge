using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class RemoteHome
    {
        [JsonProperty("activated")]
        public bool Activated { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
