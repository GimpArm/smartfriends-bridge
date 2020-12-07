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
        public int RoomId { get; set; }

        [JsonProperty("roomName")]
        public string RoomName { get; set; }

        [JsonProperty("counter")]
        public long RoomTimestamp { get; set; }

        public string GetCleanName()
        {
            return !RoomName.StartsWith("${") ? RoomName : RoomName[2..^1];
        }

        public override bool Equals(object obj)
        {
            return obj is RoomInfo room && room.RoomId == RoomId;
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return RoomId.GetHashCode();
        }
    }
}
