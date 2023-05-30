using Newtonsoft.Json;

namespace SmartFriends.Api.Models.Commands
{
    public abstract class CommandBase
    {
        [JsonProperty("command")]
        public string Command { get; }

        [JsonProperty("sessionID")]
        public string SessionId { get; set; }

        protected CommandBase(string command, string sessionId = null)
        {
            Command = command;
            SessionId = sessionId;
        }

        public virtual bool SkipEnsure => false;

        public abstract bool IsReponse(Message message);
    }
}
