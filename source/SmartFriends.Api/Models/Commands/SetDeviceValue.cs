using Newtonsoft.Json;
using System;

namespace SmartFriends.Api.Models.Commands
{
    public class SetDeviceValue : CommandBase
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
        public override bool IsReponse(Message message) => throw new NotImplementedException($"Cannot wait for reponse from command {GetType()}");
    }
}
