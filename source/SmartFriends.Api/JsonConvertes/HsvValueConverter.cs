using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartFriends.Api.Models;

namespace SmartFriends.Api.JsonConvertes
{
    public class HsvValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(FuzzyValue);
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return false;
            object result;
            if (reader.TokenType == JsonToken.StartObject)
            {
                var token = JToken.Load(reader);
                result = token.ToObject<HsvValue>();
            }
            else
            {
                var jValue = new JValue(reader.Value);
                switch (reader.TokenType)
                {
                    case JsonToken.String:
                        result = Convert.ToString(jValue, CultureInfo.InvariantCulture);
                        break;
                    case JsonToken.Date:
                        result = (DateTime) jValue;
                        break;
                    case JsonToken.Boolean:
                        result = Convert.ToBoolean(jValue);
                        break;
                    case JsonToken.Integer:
                        result = Convert.ToInt32(jValue);
                        break;
                    case JsonToken.Float:
                        result = Convert.ToDecimal(jValue);
                        break;
                    default:
                        result = Convert.ToString(jValue, CultureInfo.InvariantCulture);
                        break;
                }
            }

            return new FuzzyValue(result);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}