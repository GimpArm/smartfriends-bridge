using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class NewInfoResult
    {
        [JsonProperty("currentTimestamp")]
        public long CurrentTimestamp { get; set; }

        [JsonProperty("newDeviceInfos")]
        public DeviceInfo[] DeviceInfo { get; set; }

        [JsonProperty("newDeviceInfos")]
        public DeviceValue[] DeviceValue { get; set; }

        [JsonProperty("newRoomInfos")]
        public RoomInfo[] RoomInfo { get; set; }
    }
}
