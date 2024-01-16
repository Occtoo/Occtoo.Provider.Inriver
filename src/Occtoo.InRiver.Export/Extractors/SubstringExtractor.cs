using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class SubstringExtractor : IExceptionFieldExtractor
    {
        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            if (!int.TryParse(settings.Params, out var offset)) return;

            var field = inRiverEntity.GetField(settings.Id);
            if (field?.Data == null || string.IsNullOrEmpty(field.Data.ToString())) return;

            var value = field.Data.ToString();

            if (offset == 0)
            {
                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = string.Empty,
                    Value = value
                });
            }

            string propValue;
            if (offset > 0)
            {
                propValue = offset >= value.Length
                    ? value
                    : value.Substring(offset);
            }
            else
            {
                propValue = offset + value.Length <= 0
                    ? value
                    : value.Substring(value.Length + offset);
            }

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = propValue
            });
        }
    }
}
