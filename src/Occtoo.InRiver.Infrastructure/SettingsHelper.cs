using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Occtoo.Generic.Infrastructure
{
    public static class SettingsHelper
    {
        public static Dictionary<string, string> AsDefaultSettings<T>() where T : new()
        {
            var defaultSettings = Activator.CreateInstance<T>();
            return AsSettingsDictionary(defaultSettings);
        }

        public static Dictionary<string, string> AsSettingsDictionary<T>(T item) where T : new()
        {
            var publicProperties = typeof(T).GetRuntimeProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .ToList();

            return publicProperties
                .ToDictionary(key => key.Name, value =>
                {
                    if (value.GetCustomAttribute<JsonAttribute>() != null)
                    {
                        return JsonConvert.SerializeObject(value.GetValue(item), Formatting.Indented, new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        });
                    }
                    else
                    {
                        return value.GetValue(item)?.ToString();
                    }
                });
        }

        public static T ConvertFromSettingsDictionary<T>(Dictionary<string, string> settingsDictionary) where T : new()
        {
            var settings = Activator.CreateInstance<T>();
            if (settingsDictionary == null) return settings;

            var publicProperties = typeof(T).GetRuntimeProperties()
                .Where(p => p.CanRead && p.CanWrite)
                .ToList();

            foreach (var property in publicProperties)
            {
                if (settingsDictionary.ContainsKey(property.Name))
                {
                    var settingValue = settingsDictionary[property.Name];

                    if (property.GetCustomAttribute<JsonAttribute>() != null)
                    {
                        property.SetValue(settings, JsonConvert.DeserializeObject(settingValue, property.PropertyType, new JsonSerializerSettings
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver(),
                            Converters = new List<JsonConverter>
                            {
                                new StringEnumConverter()
                            }
                        }));
                    }
                    else
                    {
                        var convertedValue = !string.IsNullOrWhiteSpace(settingValue)
                            ? TypeDescriptor.GetConverter(property.PropertyType)
                                .ConvertFromString(settingsDictionary[property.Name])
                            : default;

                        property.SetValue(settings, convertedValue);
                    }
                }
            }

            return settings;
        }
    }
}