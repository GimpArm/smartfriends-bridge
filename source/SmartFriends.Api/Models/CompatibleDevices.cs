using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class CompatibleDevices
    {
        [JsonProperty("compatibleDevices")]
        public DeviceDefinition[] Definitions { get; set; }
    }
}
