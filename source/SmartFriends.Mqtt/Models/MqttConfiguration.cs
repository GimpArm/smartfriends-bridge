namespace SmartFriends.Mqtt.Models;

public class MqttConfiguration
{
    public bool Enabled { get; set; }
    public string DataPath { get; set; } = string.Empty;
    public string BaseTopic { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public string? ProtocolVersion { get; set; } = null;
}