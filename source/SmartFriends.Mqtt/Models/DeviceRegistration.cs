using Newtonsoft.Json;

namespace SmartFriends.Mqtt.Models
{
    public class DeviceRegistration
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("command_topic")]
        public string CommandTopic { get; set; }

        [JsonProperty("state_topic")]
        public string StateTopic { get; set; }

        [JsonProperty("qos")]
        public int Qos { get; set; }

        [JsonProperty("availability")]
        public Availability Availability { get; set; }

        [JsonProperty("device")]
        public Device Device { get; set; }

        [JsonProperty("device_class")]
        public string DeviceClass { get; set; }

        [JsonProperty("json_attributes_topic")]
        public string JsonAttributesTopic { get; set; }

        [JsonProperty("json_attributes_template")]
        public string JsonAttributesTemplate { get; set; }

        [JsonProperty("unique_id")]
        public string UniqueId { get; set; }

        [JsonProperty("value_template")]
        public string ValueTemplate { get; set; }
    }
}
