using Newtonsoft.Json;

namespace SmartFriends.Api.Models.Commands
{
    public class SetDeviceValue: CommandBase
    {
        [JsonProperty("deviceID")]
        public int DeviceId { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        public SetDeviceValue(int deviceId, int value) : base("setDeviceValue")
        {
            DeviceId = deviceId;
            Value = value;
        }
    }
}
