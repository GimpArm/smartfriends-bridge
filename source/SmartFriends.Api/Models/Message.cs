using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartFriends.Api.Models
{
    public class Message
    {
        [JsonProperty("response")]
        public JObject Response { get; set; }

        [JsonProperty("responseCode")]
        public int ResponseCode { get; set; }

        [JsonProperty("responseMessage")]
        public string ResponseMessage { get; set; }
    }
}
