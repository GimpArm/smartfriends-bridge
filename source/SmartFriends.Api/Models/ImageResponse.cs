using Newtonsoft.Json;

namespace SmartFriends.Api.Models;

public class ImageResponse
{

    [JsonProperty("id")]
    public int DeviceId { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("image")]
    public ImageData Image { get; set; } = new();
}