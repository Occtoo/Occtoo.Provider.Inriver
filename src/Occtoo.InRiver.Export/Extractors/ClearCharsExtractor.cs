using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Helpers;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System.Collections.Generic;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class ClearCharsExtractor : IExceptionFieldExtractor
    {
        private readonly Settings _settings;
        private readonly inRiverContext _context;

        public ClearCharsExtractor(Settings settings, inRiverContext context)
        {
            _settings = settings;
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var field = inRiverEntity.GetField(settings.Id);
            if (field == null) return;

            if (field.FieldType.DataType == DataType.LocaleString)
            {
                dynamicEntity.Properties.AddRange(ClearCharsLocalized(field,
                    string.IsNullOrEmpty(settings.Alias)
                        ? field.FieldType.Id
                        : settings.Alias, settings.Params));
            }
            else
            {
                dynamicEntity.Properties.Add(ValueHelpers.GetValue(settings.Alias,
                    ValueHelpers.ClearChars(field.Data?.ToString(), settings.Params), string.Empty));
            }
        }

        private IEnumerable<DynamicProperty> ClearCharsLocalized(Field field, string propertyName, string parameters)
        {
            var dynamicProperties = field.Data == null && field.Revision > 0
                ? ValueHelpers.GetEmptyValuesForLocaleString(field, _context, propertyName)
                : ValueHelpers.GetValuesForLocaleStringFieldWithFallbackToDefault(field, _settings.DefaultLanguage, propertyName);

            foreach (var dynamicProperty in dynamicProperties)
            {
                dynamicProperty.Value = ValueHelpers.ClearChars(dynamicProperty.Value, parameters);
            }

            return dynamicProperties;
        }
    }
}