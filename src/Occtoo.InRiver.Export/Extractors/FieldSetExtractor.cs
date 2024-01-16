using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class FieldSetExtractor : IExceptionFieldExtractor
    {
        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            if (!string.IsNullOrEmpty(inRiverEntity.FieldSetId))
            {
                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = string.Empty,
                    Value = inRiverEntity.FieldSetId
                });
            }
        }
    }
}