using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class FieldValuesEqualExtractor : IExceptionFieldExtractor
    {
        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var parsedParams = settings.Params.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            if (parsedParams.Length != 2 || !bool.TryParse(parsedParams[1], out var extractIfEmptyValues)) return;

            var fieldValue = inRiverEntity.GetField(settings.Id)?.Data?.ToString();
            var comparingFieldValue = inRiverEntity.GetField(parsedParams[0])?.Data?.ToString();

            if (!extractIfEmptyValues &&
               (string.IsNullOrEmpty(fieldValue) || string.IsNullOrEmpty(comparingFieldValue)))
            {
                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = string.Empty,
                    Value = null
                });
                return;
            }

            var equal = !string.IsNullOrEmpty(fieldValue) &&
                        !string.IsNullOrEmpty(comparingFieldValue) &&
                        string.Equals(fieldValue, comparingFieldValue, StringComparison.InvariantCulture);

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = equal.ToString()
            });
        }
    }
}