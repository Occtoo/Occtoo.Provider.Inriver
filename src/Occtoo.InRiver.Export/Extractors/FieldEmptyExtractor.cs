using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class FieldEmptyExtractor : IExceptionFieldExtractor
    {
        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var isFieldEmpty = false;
            var fieldEmpty = inRiverEntity.GetField(settings.Id);
            if (fieldEmpty == null)
            {
                isFieldEmpty = true;
            }

            if (!isFieldEmpty)
            {
                isFieldEmpty = string.IsNullOrEmpty(fieldEmpty.Data?.ToString());
            }

            if (settings.Params == "inverse")
            {
                isFieldEmpty = !isFieldEmpty;
            }

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = isFieldEmpty.ToString()
            });
        }
    }
}