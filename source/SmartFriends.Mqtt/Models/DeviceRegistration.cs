using Newtonsoft.Json;

namespace SmartFriends.Mqtt.Models;

public class DeviceRegistration
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("command_topic")]
    public string CommandTopic { get; set; } = string.Empty;

    [JsonProperty("state_topic")]
    public string StateTopic { get; set; } = string.Empty;

    [JsonProperty("qos")]
    public int Qos { get; set; }

    [JsonProperty("availability")]
    public Availability Availability { get; set; } = new();

    [JsonProperty("device")]
    public Device Device { get; set; } = new();

    [JsonProperty("device_class")]
    public string? DeviceClass { get; set; }

    [JsonProperty("json_attributes_topic")]
    public string JsonAttributesTopic { get; set; } = string.Empty;

    [JsonProperty("json_attributes_template")]
    public string? JsonAttributesTemplate { get; set; }

    [JsonProperty("unique_id")]
    public string UniqueId { get; set; } = string.Empty;

    [JsonProperty("value_template")]
    public string? ValueTemplate { get; set; }
}