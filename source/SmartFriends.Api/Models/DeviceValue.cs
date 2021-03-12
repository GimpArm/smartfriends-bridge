using Newtonsoft.Json;
using SmartFriends.Api.JsonConvertes;

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
        [JsonConverter(typeof(HsvValueConverter))]
        public FuzzyValue Value { get; set; }

        [JsonProperty("valueTimestamp")]
        public long ValueTimestamp { get; set; }
    }

}
