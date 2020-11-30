using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class GatewayInfo
    {
        [JsonProperty("aR")]
        public bool AR { get; set; }

        [JsonProperty("hardware")]
        public string Hardware { get; set; }

        [JsonProperty("localSHServerTime")]
        public string LocalSHServerTime { get; set; }

        [JsonProperty("macAddress")]
        public string MacAddress { get; set; }

        [JsonProperty("pushNotificationUrl")]
        public string PushNotificationUrl { get; set; }

        [JsonProperty("sessionId")]
        public string SessionId { get; set; }

        [JsonProperty("shsVersion")]
        public string ShsVersion { get; set; }

        [JsonProperty("webshopUrl")]
        public string WebshopUrl { get; set; }

        [JsonProperty("remoteHome")]
        public RemoteHome RemoteHome { get; set; }
    }
}
