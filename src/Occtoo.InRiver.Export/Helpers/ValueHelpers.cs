using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Services;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Occtoo.Generic.Inriver.Helpers
{
    public static class ValueHelpers
    {
        public static DynamicProperty GetValue(string id, object value, string language)
        {
            var dataValue = new DynamicProperty { Id = id, Language = language };

            switch (value)
            {
                case null:
                    return dataValue;

                case string _:
                    dataValue.Value = value.ToString();
                    break;

                case DateTime time:
                    dataValue.Value = time.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    break;

                case bool b:
                    dataValue.Value = b.ToString();
                    break;

                case int i:
                    dataValue.Value = i.ToString();
                    break;

                case double d:
                    dataValue.Value = d.ToString(CultureInfo.InvariantCulture);
                    break;
            }

            return dataValue;
        }

        public static string GetCvlKeys(object data)
        {
            return data == null ? string.Empty : data.ToString().Replace(Constants.InRiver.MultiValueSeparator, Constants.Occtoo.MultiValueDefaultSeparator);
        }

        public static List<DynamicProperty> GetValuesForLocaleStringFieldWithFallbackToDefault(Field field, string defaultLanguage, string customPropertyName = null)
        {
            var values = new List<DynamicProperty>();

            if (!(field.Data is LocaleString localString)) return values;

            foreach (var ci in localString.Languages)
            {
                var value = localString[ci];

                if (string.IsNullOrEmpty(value))
                {
                    value = localString[new CultureInfo(defaultLanguage)];
                }

                var propertyName = !string.IsNullOrWhiteSpace(customPropertyName)
                    ? customPropertyName
                    : field.FieldType.Id;
                values.Add(GetValue(propertyName, value, ci.Name));
            }

            return values;
        }

        public static List<DynamicProperty> GetEmptyValuesForLocaleString(Field field, inRiverContext context, string customPropertyName = null)
        {
            var propertyName = !string.IsNullOrWhiteSpace(customPropertyName)
                ? customPropertyName
                : field.FieldType.Id;

            var languages = context.ExtensionManager.UtilityService.GetAllLanguages();

            return languages.Select(ci => ValueHelpers.GetValue(propertyName, null, ci.Name)).ToList();
        }

        public static string ClearChars(string data, string parameters)
        {
            if (string.IsNullOrEmpty(data)) return string.Empty;

            if (string.IsNullOrEmpty(parameters)) return data;

            var chars = parameters.ToCharArray();

            return chars.Aggregate(data, (current, c) => current.Replace(c.ToString(), ""));
        }

        public static string GetMediaId(Entity resource, List<string> uniqueIdFields, string mediaNamespace)
        {
            if (uniqueIdFields == null)
            {
                // Fallback if no uinqueIdFields are set
                return resource.Id.ToString();
            }

            var uniqueKey = string.Empty;
            foreach (var field in uniqueIdFields)
            {
                var fieldData = resource.GetField(field)?.Data;
                if (string.IsNullOrEmpty(fieldData?.ToString()))
                {
                    continue;
                }

                uniqueKey += $"{fieldData}_";
            }

            uniqueKey = uniqueKey.TrimEnd('_');

            if (string.IsNullOrEmpty(uniqueKey))
            {
                // Fallback if no uinqueIdFields are found or empty
                return resource.Id.ToString();
            }

            return GuidUtility.Create(new Guid(mediaNamespace), uniqueKey).ToString();
        }

        public static bool GetResourceFilename(Entity resource, string documentIdForbiddenChars, out string filename)
        {
            filename = string.Empty;
            var file = resource.GetField("ResourceFilename").Data;
            if (file == null)
            {
                return false;
            }

            filename = file.ToString();
            if (string.IsNullOrEmpty(filename))
            {
                return false;
            }

            foreach (var forbiddenChar in GetForbiddenChars(documentIdForbiddenChars))
            {
                filename = filename.Replace(forbiddenChar.Key, forbiddenChar.Value);
            }

            return true;
        }

        private static Dictionary<string, string> GetForbiddenChars(string documentIdForbiddenChars)
        {
            var response = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(documentIdForbiddenChars)) return response;

            var charAndReplace = documentIdForbiddenChars.Split(new[] { '|' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var cr in charAndReplace)
            {
                var chars = cr.Split(new[] { ';' }, StringSplitOptions.None);
                if (chars.Length == 2)
                {
                    response.Add(chars[0], chars[1]);
                }
            }

            return response;
        }
    }
}