using System;

namespace SmartFriends.Api.Models.Commands
{
    public class KeepAlive : CommandBase
    {
        public KeepAlive() : base("keepalive") { }

        public override bool IsReponse(Message message) => throw new NotImplementedException($"Cannot wait for reponse from command {GetType()}");
    }
}
