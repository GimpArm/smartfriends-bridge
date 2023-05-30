using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartFriends.Api.Models.Commands
{
    public class Hello : CommandBase
    {
        [JsonProperty("username")]
        public string Username { get; }

        public Hello(string username) : base("helo")
        {
            Username = username;
        }
        public override bool SkipEnsure => true;
        public override bool IsReponse(Message message) => !string.IsNullOrEmpty(message.Response?["salt"]?.Value<string>());
    }
}
