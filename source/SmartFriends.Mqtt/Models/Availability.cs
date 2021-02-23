using Newtonsoft.Json;

namespace SmartFriends.Mqtt.Models
{
    public class Availability
    {
        [JsonProperty("topic")]
        public string Topic { get; set; }
    }
}
