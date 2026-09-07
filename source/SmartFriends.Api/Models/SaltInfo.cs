using Newtonsoft.Json;

namespace SmartFriends.Api.Models;

public class SaltInfo
{
    [JsonProperty("salt")]
    public string Salt { get; set; } = string.Empty;

    [JsonProperty("sessionSalt")]
    public string SessionSalt { get; set; } = string.Empty;
}