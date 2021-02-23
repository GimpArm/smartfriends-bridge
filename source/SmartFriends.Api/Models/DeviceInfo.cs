using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class DeviceInfo
    {
        [JsonProperty("deviceID")]
        public int DeviceId { get; set; }

        [JsonProperty("deviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("masterDeviceID")]
        public int MasterDeviceId { get; set; }

        [JsonProperty("masterDeviceName")]
        public string MasterDeviceName { get; set; }

        [JsonProperty("deviceDesignation")]
        public string DeviceDesignation { get; set; }

        [JsonProperty("deviceTypClient")]
        public string DeviceTypClient { get; set; }

        [JsonProperty("deviceTypServer")]
        public string DeviceTypServer { get; set; }

        [JsonProperty("firstLevel")]
        public bool FirstLevel { get; set; }

        [JsonProperty("roomID")]
        public int RoomId { get; set; }

        [JsonProperty("manufacturer")]
        public string Manufacturer { get; set; }

        [JsonProperty("productDesignation")]
        public string ProductDesignation { get; set; }

        public DeviceDefinition Definition { get; set; }
    }
}
