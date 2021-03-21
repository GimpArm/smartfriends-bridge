using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartFriends.Api.JsonConvertes
{
    public class HasHsvValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(bool);
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return false;

            if (reader.TokenType == JsonToken.StartObject)
            {
                JToken.Load(reader);
                return true;
            }
            return false;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
