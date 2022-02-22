using System.Collections.Generic;

namespace SmartFriends.Mqtt.Models
{
    public class TypeTemplate
    {
        public string Type { get; set; }
        public string Class { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}