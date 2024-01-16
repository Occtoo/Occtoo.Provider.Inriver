using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class FieldContainsValuesExtractor : IExceptionFieldExtractor
    {
        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var contains = false;

            var field = inRiverEntity.GetField(settings.Id);
            if (field != null &&
                !string.IsNullOrEmpty(field.FieldType.CVLId) &&
                field.Data != null &&
                !string.IsNullOrEmpty(settings.Params))
            {
                var values = field.Data.ToString().Split(new[] { Constants.InRiver.MultiValueSeparator },
                    StringSplitOptions.RemoveEmptyEntries).ToList();
                var settingsParams = settings.Params.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                if (settingsParams.Length == 2)
                {
                    var rule = settingsParams[0];
                    var expectedValues = settingsParams[1].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (expectedValues.Any())
                    {
                        switch (rule)
                        {
                            case "all":
                                contains = expectedValues.All(ev => values.Contains(ev));
                                break;

                            case "any":
                                contains = expectedValues.Any(ev => values.Contains(ev));
                                break;
                        }
                    }
                }
            }

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = contains.ToString()
            });
        }
    }
}