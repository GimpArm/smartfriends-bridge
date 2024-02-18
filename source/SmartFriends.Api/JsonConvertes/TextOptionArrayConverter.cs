using Newtonsoft.Json;
using SmartFriends.Api.Helpers;
using SmartFriends.Api.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartFriends.Api.JsonConvertes
{
    public class TextOptionArrayConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(TextOption[]);
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            var result = new List<TextOption>();
            var i = 0L;
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.EndArray) break;
                TextOption option;
                if (reader.TokenType == JsonToken.StartObject)
                {
                    option = serializer.Deserialize<TextOption>(reader);
                }
                else
                {
                    option = new TextOption
                    {
                        Name = reader.Value.ToString(),
                        Value = i
                    };
                    i++;
                }

                option.Name = option.Name?.RemoveLanguageLookup().Split('.').LastOrDefault();
                result.Add(option);
            }
            return result.Any() ? result.ToArray() : null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
