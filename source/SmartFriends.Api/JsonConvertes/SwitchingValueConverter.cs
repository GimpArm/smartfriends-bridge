using Newtonsoft.Json;
using SmartFriends.Api.Models;
using System;

namespace SmartFriends.Api.JsonConvertes
{
    public class SwitchingValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(SwitchingValue);
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return 0L;

            switch (reader.Value.ToString().ToLower().Trim())
            {
                case "true":
                case "yes":
                case "y":
                case "1":
                case "switchactuatoron":
                case "lockingsystemlocked":
                case "shutterup":
                    return 1L;
                case "false":
                case "no":
                case "n":
                case "0":
                case "switchactuatoroff":
                case "lockingsystemunlocked":
                case "shutterdown":
                    return 0L;
                case "shutterstop":
                    return 2L;
            }

            return Convert.ToInt64(reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
