using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Infrastructure.Extensions;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class HasChildrenByFieldValueExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public HasChildrenByFieldValueExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var linkTypeId = settings.Id;
            if (string.IsNullOrEmpty(settings.Params)) return;

            var paramValues = settings.Params.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            if (paramValues.Length < 3) return;

            var fieldId = paramValues[0];
            var matchingPattern = paramValues[1];
            var matchingValues = paramValues.Skip(2).ToList();

            var childEntities = _context.ExtensionManager.GetChildEntities(inRiverEntity, linkTypeId);

            var responseValue = false;
            foreach (var childEntity in childEntities)
            {
                var field = childEntity.GetField(fieldId);
                if (field?.Data == null)
                {
                    if (matchingPattern == "anything")
                    {
                        dynamicEntity.Properties.Add(new DynamicProperty
                        {
                            Id = settings.Alias,
                            Language = string.Empty,
                            Value = "false"
                        });
                    }
                    continue;
                }

                var strData = field.Data.ToString();
                switch (matchingPattern)
                {
                    case "any":
                        if (!field.FieldType.Multivalue) continue;
                        responseValue = matchingValues.Intersect(strData.Split(new[] { ';' },
                            StringSplitOptions.RemoveEmptyEntries)).Any();
                        break;

                    case "all":
                        if (!field.FieldType.Multivalue) continue;
                        responseValue = matchingValues.EqualsIgnoreOrder(strData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
                        break;

                    case "anything":
                        responseValue = !string.IsNullOrEmpty(strData);
                        break;

                    default:
                        responseValue = string.Equals(strData, matchingValues.First(),
                            StringComparison.InvariantCultureIgnoreCase);
                        break;
                }

                if (responseValue) break;
            }

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = responseValue.ToString()
            });
        }
    }
}