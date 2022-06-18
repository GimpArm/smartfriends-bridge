using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartFriends.Api.Helpers;
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
            if (!mqttConfig.Enabled)
            {
                _templates = Array.Empty<TypeTemplate>();
                _baseTopic = string.Empty;
                return;
            }
            if (!string.IsNullOrWhiteSpace(mqttConfig.DataPath))
            {
                Directory.CreateDirectory(mqttConfig.DataPath);
            }
            _templates = LoadTemplates(Path.Combine(mqttConfig.DataPath, TypeTemplateFile));
            _baseTopic = mqttConfig.BaseTopic;
        }

        private static TypeTemplate[] LoadTemplates(string path)
        {
            if (File.Exists(path))
            {
                var templates = JsonConvert.DeserializeObject<TypeTemplate[]>(File.ReadAllText(path));
                var coverTemplate = templates.FirstOrDefault(x => x.Type == "cover" && x.Class == "shutter" && x.Parameters["state_stopped"] as string == "Stop");
                if (coverTemplate != null)
                {
                    //Upgrade template
                    coverTemplate.Parameters["state_stopped"] = 0;
                    coverTemplate.Parameters["state_opening"] = 1;
                    coverTemplate.Parameters["state_closing"] = 2;
                    File.WriteAllText(path, templates.Serialize());
                }
                return templates;
            }

            var template = new[]
            {
                new TypeTemplate
                {
                    Type = "cover",
                    Class = "shutter",
                    Parameters = new()
                    {
                        {"command_topic", "{baseTopic}/{deviceId}/rollingShutter/set"},
                        {"position_topic", "{baseTopic}/{deviceId}/position"},
                        {"position_template", "{{ 100 - value | int }}"},
                        {"set_position_topic", "{baseTopic}/{deviceId}/position/set"},
                        {"set_position_template", "{{ 100 - position }}"},
                        {"state_stopped", 0},
                        {"state_opening", 1 },
                        {"state_closing", 2 },
                        {"payload_stop", "Stop"},
                        {"payload_open", "Up"},
                        {"payload_close", "Down"}
                    }
                },
                new TypeTemplate
                {
                    Type = "switch",
                    Parameters = new()
                    {
                        {"state_on", 1 },
                        {"state_off", 0 },
                        {"payload_on", 1 },
                        {"payload_off", 0 },
                        {"assumed_state", true }
                    }
                }
            };
            File.WriteAllText(path, template.Serialize());
            return template;
        }

        public void Merge(JObject payload, DeviceMap map, string deviceId)
        {
            var template = GetTemplate(map);
            Merge(payload, template, deviceId);
            Merge(payload, map.Parameters, deviceId);
        }

        private Dictionary<string, object> GetTemplate(DeviceMap map)
        {
            var template = _templates.FirstOrDefault(x =>
                               string.Equals(x.Type, map.Type, StringComparison.InvariantCultureIgnoreCase) && string.Equals(x.Class, map.Class, StringComparison.InvariantCultureIgnoreCase))
                           ?? _templates.FirstOrDefault(x => string.Equals(x.Type, map.Type, StringComparison.InvariantCultureIgnoreCase));

            return template?.Parameters;
        }

        private void Merge(JObject payload, Dictionary<string, object> parameters, string deviceId)
        {
            if (parameters == null) return;

            foreach (var kvp in parameters)
            {
                JToken value;
                if (kvp.Value == null)
                {
                    value = null;
                }
                else if (kvp.Value is bool boolVal)
                {
                    value = boolVal;
                }
                else if (kvp.Value is long longVal)
                {
                    value = longVal;
                }
                else if (kvp.Value is double decimalVal)
                {
                    value = decimalVal;
                }
                else if (kvp.Value is DateTime dateVal)
                {
                    value = dateVal;
                }
                else if (kvp.Value is JToken tokenVal)
                {
                    value = tokenVal;
                }
                else
                {
                    value = ReplaceVariables(kvp.Value.ToString(), deviceId, _baseTopic);
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
