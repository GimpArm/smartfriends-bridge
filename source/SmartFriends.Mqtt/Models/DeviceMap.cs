namespace SmartFriends.Mqtt.Models;

public class DeviceMap
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Class { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
}