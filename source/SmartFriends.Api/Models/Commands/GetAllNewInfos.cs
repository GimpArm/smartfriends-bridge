using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartFriends.Api.Models.Commands
{
    public class GetAllNewInfos : CommandBase
    {
        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("compatibilityConfigurationVersion")]
        public string CCVersion { get; set; }

        [JsonProperty("languageTranslationVersion")]
        public string LTVersion { get; set; }

        public GetAllNewInfos(long timestamp = 0, string ccVersion = "0", string ltVersion = "0") : base("getAllNewInfos")
        {
            Timestamp = timestamp;
            CCVersion = ccVersion;
            LTVersion = ltVersion;
        }

        public override bool IsReponse(Message message) => (message.Response?["currentTimestamp"]?.Value<long>() ?? 0) > 0;
    }
}
