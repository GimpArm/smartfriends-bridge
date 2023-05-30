using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartFriends.Api.Models.Commands
{
    public class Login : CommandBase
    {
        [JsonProperty("username")]
        public string Username { get; }

        [JsonProperty("digest")]
        public string Digest { get; }

        [JsonProperty("cSymbol")]
        public string CSymbol { get; }

        [JsonProperty("shcVersion")]
        public string ShcVersion { get; }

        [JsonProperty("shApiVersion")]
        public string ShApiVersion { get; }

        public Login(string username, string digest, string cSynmbol, string shcVersion, string shApiVersion) : base("login")
        {
            Username = username;
            Digest = digest;
            CSymbol = cSynmbol;
            ShcVersion = shcVersion;
            ShApiVersion = shApiVersion;
        }
        public override bool SkipEnsure => true;
        public override bool IsReponse(Message message) => !string.IsNullOrEmpty(message.Response?["sessionID"]?.Value<string>());
    }
}
