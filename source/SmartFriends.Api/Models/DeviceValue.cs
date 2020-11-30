using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class DeviceValue
    {
        [JsonProperty("counter")]
        public int Counter { get; set; }

        [JsonProperty("deviceID")]
        public int DeviceID { get; set; }

        [JsonProperty("MasterDeviceID")]
        public int MasterDeviceID { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonProperty("valueTimestamp")]
        public long ValueTimestamp { get; set; }
    }

}
