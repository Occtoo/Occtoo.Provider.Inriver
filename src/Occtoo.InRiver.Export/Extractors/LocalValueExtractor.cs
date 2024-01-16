using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class LocalValueExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public LocalValueExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var field = inRiverEntity.GetField(settings.Id);
            if (field?.Data == null)
            {
                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = string.Empty,
                    Value = string.Empty
                });
                return;
            };

            var dynamicProperty = new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = string.Empty
            };
            if (field.FieldType.DataType == "CVL")
            {
                var cvlValues = GetCvlValues(field);
                if (!cvlValues.ContainsKey(settings.Params))
                {
                    dynamicEntity.Properties.Add(dynamicProperty);
                    return;
                }

                if (string.IsNullOrEmpty(cvlValues[settings.Params]))
                {
                    dynamicEntity.Properties.Add(dynamicProperty);
                    return;
                }

                dynamicProperty.Value = cvlValues[settings.Params];
            }
            else
            {
                if (!(field.Data is LocaleString localString))
                {
                    dynamicEntity.Properties.Add(dynamicProperty);
                    return;
                }

                var localValue = localString[new CultureInfo(settings.Params)];

                if (string.IsNullOrEmpty(localValue))
                {
                    dynamicEntity.Properties.Add(dynamicProperty);
                    return;
                }

                dynamicProperty.Value = localValue;
            }
            dynamicEntity.Properties.Add(dynamicProperty);
        }

        private Dictionary<string, string> GetCvlValues(Field field)
        {
            var localeValues = new Dictionary<string, string>();

            var cvlKeys = field.Data?.ToString().Split(new[] { Constants.InRiver.MultiValueSeparator }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            foreach (var key in cvlKeys)
            {
                var value = _context.ExtensionManager.ModelService.GetCVLValueByKey(key, field.FieldType.CVLId);

                if (!(value?.Value is LocaleString ls)) continue;

                foreach (var ci in ls.Languages)
                {
                    var localeValue = ls[ci];

                    if (string.IsNullOrEmpty(localeValue))
                    {
                        continue;
                    }

                    if (!localeValues.ContainsKey(ci.Name))
                    {
                        localeValues.Add(ci.Name, localeValue);
                    }
                    else
                    {
                        localeValues[ci.Name] = localeValues[ci.Name] + Constants.Occtoo.MultiValueDefaultSeparator + localeValue;
                    }
                }
            }

            return localeValues;
        }
    }
}