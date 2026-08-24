using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class ImageData
    {
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }

        [JsonProperty("timestamp")]
        public long TimeStamp { get; set; }

        [JsonProperty("data")]
        public byte[] Data { get; set; }
    }
}
