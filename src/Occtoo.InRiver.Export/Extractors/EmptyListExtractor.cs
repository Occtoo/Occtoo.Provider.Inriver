using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class EmptyListExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public EmptyListExtractor(inRiverContext context)
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
                    Value = settings.Params
                });
            }
            else
            {
                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = string.Empty,
                    Value = GetCvlValues(field)
                });
            }
        }

        private string GetCvlValues(Field field)
        {
            var response = string.Empty;

            var cvlKeys = field.Data?.ToString().Split(new[] { Constants.InRiver.MultiValueSeparator }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            foreach (var key in cvlKeys)
            {
                var value = _context.ExtensionManager.ModelService.GetCVLValueByKey(key, field.FieldType.CVLId);

                if (value == null) continue;

                if (string.IsNullOrEmpty(response))
                {
                    response = value.Value.ToString();
                }
                else
                {
                    response = response + Constants.Occtoo.MultiValueDefaultSeparator + value.Value;
                }
            }

            return response;
        }
    }
}