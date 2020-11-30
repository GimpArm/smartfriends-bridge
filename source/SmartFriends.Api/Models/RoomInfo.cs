using Newtonsoft.Json;

namespace SmartFriends.Api.Models
{
    public class RoomInfo
    {
        [JsonProperty("favorite")]
        public bool Favorite { get; set; }

        [JsonProperty("iconName")]
        public string IconName { get; set; }

        [JsonProperty("roomID")]
        public int RoomID { get; set; }

        [JsonProperty("roomName")]
        public string RoomName { get; set; }

        [JsonProperty("counter")]
        public long RoomTimestamp { get; set; }

        public string GetCleanName()
        {
            return !RoomName.StartsWith("${") ? RoomName : RoomName.Substring(2, RoomName.Length - 3);
        }

        public override bool Equals(object obj)
        {
            return obj is RoomInfo room && room.RoomID == RoomID;
        }
    }
}
