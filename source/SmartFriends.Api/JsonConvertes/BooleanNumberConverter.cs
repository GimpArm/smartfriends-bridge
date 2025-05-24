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
                case "burglar":
                case "true":
                case "yes":
                case "y":
                case "1":
                    {
                        return 1L;
                    }
                case "clear":
                case "false":
                case "no":
                case "n":
                case "0":
                    {
                        return 0L;
                    }
            }
            try
            {
                return Convert.ToInt64(reader.Value);
            }
            catch
            {
                Console.Write($"Unknown Boolean Value: {reader.Value}");
                return 0L;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
