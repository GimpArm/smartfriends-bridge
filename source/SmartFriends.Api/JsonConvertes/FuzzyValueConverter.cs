using System;
using Newtonsoft.Json;
using SmartFriends.Api.Models;

namespace SmartFriends.Api.JsonConvertes
{
    public class FuzzyValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(FuzzyValue);
        public override bool CanWrite => true;
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (!(value is FuzzyValue fValue))
            {
                writer.WriteNull();
                return;
            }

            if (!fValue.IsHsv)
            {
                writer.WriteValue(fValue.Value);
                return;
            }

            var hsvValue = fValue.Value as HsvValue;
            writer.WriteStartObject();
            writer.WritePropertyName("h");
            writer.WriteValue(hsvValue?.H);
            writer.WritePropertyName("s");
            writer.WriteValue(hsvValue?.S);
            writer.WritePropertyName("v");
            writer.WriteValue(hsvValue?.V);
        }
    }
}
