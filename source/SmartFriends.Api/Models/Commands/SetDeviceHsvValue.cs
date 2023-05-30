using Newtonsoft.Json;
using System;

namespace SmartFriends.Api.Models.Commands
{
    public class SetDeviceHsvValue : CommandBase
    {
        [JsonProperty("deviceID")]
        public int DeviceId { get; set; }

        [JsonProperty("value")]
        public HsvValue Value { get; set; }

        public SetDeviceHsvValue(int deviceId, HsvValue value) : base("setDeviceValue")
        {
            DeviceId = deviceId;
            Value = value;
        }
        public override bool IsReponse(Message message) => throw new NotImplementedException($"Cannot wait for reponse from command {GetType()}");
    }
}
