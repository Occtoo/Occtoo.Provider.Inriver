using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Infrastructure.Extensions;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Enums;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class LinkedEntityLocalizedFieldValuesExtractor : IExceptionFieldExtractor
    {
        private readonly Settings _settings;
        private readonly inRiverContext _context;

        public LinkedEntityLocalizedFieldValuesExtractor(Settings setting, inRiverContext context)
        {
            _settings = setting;
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var fieldId = GetFieldId(inRiverEntity, settings.Params);
            if (string.IsNullOrEmpty(fieldId)) return;

            var linkedEntityIds = GetLinkedEntityIds(inRiverEntity, settings);

            if (!linkedEntityIds.Any()) return;

            var values = ExtractValues(linkedEntityIds, fieldId);

            foreach (var value in values)
            {
                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = value.Key,
                    Value = string.Join(Constants.Occtoo.MultiValueDefaultSeparator, value.Value)
                });
            }
        }

        private Dictionary<string, List<string>> ExtractValues(List<int> linkedEntityIds, string fieldId)
        {
            var values = new Dictionary<string, List<string>>();

            foreach (var linkedEntity in GetLinkedEntities(linkedEntityIds))
            {
                var field = linkedEntity.GetField(fieldId);
                if (field?.Data == null || field.FieldType.Multivalue) continue;

                if (field.FieldType.DataType == "CVL")
                {
                    var cvl = _context.ExtensionManager.ModelService.GetCVL(fieldId);
                    if (cvl.DataType != "LocaleString") continue;

                    var cvlValue =
                        _context.ExtensionManager.ModelService.GetCVLValueByKey(field.Data.ToString(), cvl.Id);

                    if (!(cvlValue.Value is LocaleString ls)) continue;

                    ExtractLocalizedValues(ls, values);
                }
                else
                {
                    if (!(field.Data is LocaleString ls)) continue;

                    ExtractLocalizedValues(ls, values);
                }
            }

            return values;
        }

        private void ExtractLocalizedValues(LocaleString ls, Dictionary<string, List<string>> values)
        {
            foreach (var cultureInfo in ls.Languages)
            {
                var localizedValue = ls.ContainsCulture(cultureInfo)
                    ? string.IsNullOrEmpty(ls[cultureInfo])
                        ? ls[new CultureInfo(_settings.DefaultLanguage)]
                        : ls[cultureInfo]
                    : ls[new CultureInfo(_settings.DefaultLanguage)];
                if (!values.ContainsKey(cultureInfo.Name))
                {
                    values.Add(cultureInfo.Name, new List<string>());
                }

                values[cultureInfo.Name].Add(localizedValue);
            }
        }

        private List<Entity> GetLinkedEntities(List<int> linkedEntityIds)
        {
            var response = new List<Entity>();
            foreach (var idsBatch in linkedEntityIds.Batch(500))
            {
                response.AddRange(_context.ExtensionManager.DataService.GetEntities(idsBatch.ToList(), LoadLevel.DataOnly));
            }
            return response;
        }

        private List<int> GetLinkedEntityIds(Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            List<int> linkedEntityIds;
            switch (settings.Type)
            {
                case ExceptionFieldType.ChildrenFieldLocalizedValues:
                    linkedEntityIds = _context.ExtensionManager.DataService
                        .GetOutboundLinksForEntityAndLinkType(inRiverEntity.Id, settings.Id)
                        .Select(x => x.Target.Id).ToList();
                    break;

                case ExceptionFieldType.ParentFieldLocalizedValues:
                    linkedEntityIds = _context.ExtensionManager.DataService
                        .GetInboundLinksForEntityAndLinkType(inRiverEntity.Id, settings.Id)
                        .Select(x => x.Source.Id).ToList();
                    break;

                default:
                    linkedEntityIds = new List<int>();
                    break;
            }

            return linkedEntityIds;
        }

        private static string GetFieldId(Entity inRiverEntity, string settingsParams)
        {
            if (string.IsNullOrEmpty(settingsParams)) return null;

            var settingsParamsParts = settingsParams.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

            if (settingsParamsParts.Length != 1 && settingsParamsParts.Length != 3) return null;

            if (settingsParamsParts.Length == 1) return settingsParams;

            var leadingFieldId = settingsParamsParts[0];
            var defaultFieldId = settingsParamsParts[1];

            var mappings = settingsParamsParts[2].Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            var mappingDict =
                mappings
                    .Select(mapping => mapping
                        .Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries))
                    .Where(keyValue => keyValue.Length == 2)
                    .ToDictionary(keyValue => keyValue[0], keyValue => keyValue[1]);

            var leadingFieldValue = inRiverEntity.GetField(leadingFieldId)?.Data?.ToString();

            if (string.IsNullOrEmpty(leadingFieldValue)) return defaultFieldId;

            return mappingDict.ContainsKey(leadingFieldValue)
                ? mappingDict[leadingFieldValue]
                : defaultFieldId;
        }
    }
}