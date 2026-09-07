using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SmartFriends.Api.Models.Commands;

public class ObjectAction : CommandBase
{
    [JsonProperty("id")]
    public int DeviceId { get; set; }

    [JsonProperty("action")]
    [JsonConverter(typeof(StringEnumConverter))]
    public ObjectActionEnum Action { get; set; }

    [JsonProperty("properties")]
    public Dictionary<string, object> Properties { get; } = new();

    public ObjectAction(int deviceId, ObjectActionEnum action = ObjectActionEnum.getImage, IDictionary<string, object>? properties = null) : base("objectAction")
    {
        DeviceId = deviceId;
        Action = action;
        if (properties != null)
        {
            foreach (var item in properties)
            {
                Properties.Add(item.Key, item.Value);
            }
        }
    }

    public override bool IsReponse(Message message) => message.Response?["image"] != null;
}