using System.Linq;

namespace SmartFriends.Api.Models.Commands
{
    public class Logout : CommandBase
    {
        public Logout() : base("logout") { }
        public override bool IsReponse(Message message) => message.ResponseMessage == "success" && message.Response.Children().Count() == 0;
    }
}
