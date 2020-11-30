namespace SmartFriends.Api.Models
{
    public class Configuration
    {
        public string Username { get; set; } = "Test";
        public string Password { get; set; } = "glenview";
        public string Host { get; set; } = "smartfriends.local";
        public int Port { get; set; } = 4300;
        public string CSymbol { get; set; } = "D19033";
        public string CSymbolAddon { get; set; } = "i";
        public string ShcVersion { get; set; } = "2.21.1";
        public string ShApiVersion { get; set; } = "2.20";
    }
}
