using System;
using System.Linq;
using Newtonsoft.Json;
using SmartFriends.Api.Models;

namespace SmartFriends.Api.Helpers
{
    public static class Extensions
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };


        public static string FirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        public static string RemoveLanguageLookup(this string input)
        {
            return input.StartsWith("${") ? input[2..^1] : input;
        }

        public static string Serialize(this object input)
        {
            return JsonConvert.SerializeObject(input, SerializerSettings);
        }

        public static bool IsVisible(this DeviceInfo device)
        {
            return device?.Definition?.Hidden == null || !device.Definition.Hidden.ContainsKey("deviceOverview") || !device.Definition.Hidden["deviceOverview"];
        }
    }
}