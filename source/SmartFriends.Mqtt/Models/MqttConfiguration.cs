namespace SmartFriends.Mqtt.Models
{
    public class MqttConfiguration
    {
        public string BaseTopic { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }

        public TypeTemplate[] TypeTemplates { get; set; }

        public DeviceMap[] DeviceMaps { get; set; }
    }
}
