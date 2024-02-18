using Newtonsoft.Json;
using System;

namespace SmartFriends.Api.JsonConvertes
{
    public class BooleanNumberConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(int?);
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            switch (reader.Value.ToString().ToLower().Trim())
            {
                case "true":
                case "yes":
                case "y":
                case "1":
                    return 1L;
                case "false":
                case "no":
                case "n":
                case "0":
                    return 0L;
            }

            return Convert.ToInt64(reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
