﻿using Newtonsoft.Json;
using System;
using SmartFriends.Api.Models;

namespace SmartFriends.Api.JsonConvertes
{
    public class SwitchingValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(SwitchingValue);
        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return 0;

            switch (reader.Value.ToString().ToLower().Trim())
            {
                case "true":
                case "yes":
                case "y":
                case "1":
                    return 1;
                case "false":
                case "no":
                case "n":
                case "0":
                    return 0;
            }

            return Convert.ToInt32(reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
