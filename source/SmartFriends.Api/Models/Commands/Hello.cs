using Newtonsoft.Json;

namespace SmartFriends.Api.Models.Commands
{
    public class Hello: CommandBase
    {
        [JsonProperty("username")]
        public string Username { get; }

        public Hello(string username): base("helo")
        {
            Username = username;
        }
    }
}
