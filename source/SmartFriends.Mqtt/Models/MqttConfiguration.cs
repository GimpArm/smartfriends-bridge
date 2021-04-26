namespace SmartFriends.Mqtt.Models
{
    public class MqttConfiguration
    {
        public bool Enabled { get; set; }
        public string DataPath { get; set; }
        public string BaseTopic { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public bool UseSsl { get; set; }
    }
}
