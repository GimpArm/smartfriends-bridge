namespace SmartFriends.Api.Models.Commands
{
    public class KeepAlive : CommandBase
    {
        public KeepAlive() : base("keepalive") { }

        public override bool IsReponse(Message message) => false;
    }
}
