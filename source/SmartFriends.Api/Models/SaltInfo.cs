using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class SaltInfo
    {
        [JsonProperty("salt")]
        public string Salt { get; set; }

        [JsonProperty("sessionSalt")]
        public string SessionSalt { get; set; }
    }
}
