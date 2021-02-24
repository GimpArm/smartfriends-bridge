using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SmartFriends.Api.Models;

namespace SmartFriends.Api.Helpers
{
    public static class Extensions
    {
        public static string FirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input.Substring(1);
            }
        }

        public static string Serialize(this List<DeviceMaster> list)
        {
            return JsonConvert.SerializeObject(list, Formatting.Indented);
        }
    }
}
