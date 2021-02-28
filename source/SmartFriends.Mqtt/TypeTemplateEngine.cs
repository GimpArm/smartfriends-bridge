using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartFriends.Mqtt.Models;

namespace SmartFriends.Mqtt
{
    public class TypeTemplateEngine
    {
        public const string TypeTemplateFile = "typeTemplate.json";

        private static readonly Regex DeviceIdRegEx = new Regex(@"(?<!\{)(\{deviceId\})(?!>\})", RegexOptions.IgnoreCase);
        private static readonly Regex BaseTopcRegEx = new Regex(@"(?<!\{)(\{baseTopic\})(?!>\})", RegexOptions.IgnoreCase);

        private readonly TypeTemplate[] _templates;
        private readonly string _baseTopic;

        public TypeTemplateEngine(MqttConfiguration mqttConfig)
        {
            Directory.CreateDirectory(mqttConfig.DataPath);
            _templates = LoadTemplates(Path.Combine(mqttConfig.DataPath, TypeTemplateFile));
            _baseTopic = mqttConfig.BaseTopic;
        }

        private static TypeTemplate[] LoadTemplates(string path)
        {
            if (File.Exists(path))
            {
                return JsonConvert.DeserializeObject<TypeTemplate[]>(File.ReadAllText(path));
            }

            var template = new[]
            {
                new TypeTemplate
                {
                    Type = "cover",
                    Class = "shutter",
                    Parameters = new Dictionary<string, string>
                    {
                        {"value_template", "{{ 100 - value_json.analogValue }}"},
                        {"position_topic", "{baseTopic}/{deviceId}"},
                        {"set_position_topic", "{baseTopic}/{deviceId}/set"},
                        {"set_position_template", "{{ 100 - position }}"},
                        {"payload_stop", "Stop"},
                        {"payload_open", "Up"},
                        {"payload_close", "Down"}
                    }
                },
                new TypeTemplate
                {
                    Type = "switch",
                    Parameters = new Dictionary<string, string>
                    {
                        {"value_template", "{{ value_json.state }}"}
                    }
                }
            };
            File.WriteAllText(path, JsonConvert.SerializeObject(template, Formatting.Indented));

            return template;
        }

        public void Merge(JObject payload, DeviceMap map, string deviceId)
        {
            var template = GetTemplate(map);
            Merge(payload, template, deviceId);
            Merge(payload, map.Parameters, deviceId);
        }

        private Dictionary<string, string> GetTemplate(DeviceMap map)
        {
            var template = _templates.FirstOrDefault(x =>
                               string.Equals(x.Type, map.Type, StringComparison.InvariantCultureIgnoreCase) && string.Equals(x.Class, map.Class, StringComparison.InvariantCultureIgnoreCase))
                           ?? _templates.FirstOrDefault(x => string.Equals(x.Type, map.Type, StringComparison.InvariantCultureIgnoreCase));

            return template?.Parameters;
        }

        private void Merge(JObject payload, Dictionary<string, string> parameters, string deviceId)
        {
            if (parameters == null) return;

            foreach (var kvp in parameters)
            {
                JToken value;
                if (int.TryParse(kvp.Value, out var intValue))
                {
                    value = intValue;
                }
                else if (decimal.TryParse(kvp.Value, out var decValue))
                {
                    value = decValue;
                }
                else
                {
                    value = ReplaceVariables(kvp.Value, deviceId, _baseTopic);
                }

                if (payload.ContainsKey(kvp.Key))
                {
                    payload[kvp.Key] = value;
                }
                else
                {
                    payload.Add(kvp.Key, value);
                }
            }
        }

        private static string ReplaceVariables(string input, string deviceId, string baseTopic)
        {
            return BaseTopcRegEx.Replace(DeviceIdRegEx.Replace(input, deviceId), baseTopic);
        }
    }
}
