namespace SmartFriends.Api.Models.Commands
{
    public class GetCompatibilityConfiguration : CommandBase
    {
        public GetCompatibilityConfiguration() : base("getCompatibilityConfiguration") { }

        public override bool IsReponse(Message message) => message.Response?["newCompatibilityConfiguration"] != null;
    }
}