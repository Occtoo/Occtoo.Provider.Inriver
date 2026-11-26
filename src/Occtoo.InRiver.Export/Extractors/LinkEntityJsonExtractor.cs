using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Occtoo.Generic.Infrastructure.Extensions;
using inRiver.Remoting.Log;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class LinkEntityJsonExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public LinkEntityJsonExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            if (!LinkEntityJsonSettings.TryParse(settings.Params, out var config))
            {
                _context.Logger?.Log(LogLevel.Warning, $"Invalid Params JSON for LinkEntityJson configuration '{settings.Id}' with alias '{settings.Alias}'. Skipping extraction.");
                return;
            }

            if (!config.TryGetDirection(out var isInbound))
            {
                _context.Logger?.Log(LogLevel.Warning, $"Invalid Direction '{config.Direction}' for LinkEntityJson configuration '{settings.Id}' with alias '{settings.Alias}'. Expected Inbound or Outbound. Skipping extraction.");
                return;
            }

            var dataService = _context.ExtensionManager.DataService;
            var links = isInbound ? dataService.GetInboundLinksForEntityAndLinkType(inRiverEntity.Id, settings.Id).ToList() : dataService.GetOutboundLinksForEntityAndLinkType(inRiverEntity.Id, settings.Id).ToList();
            var orderedLinks = links.Where(l => !l.Inactive).OrderBy(l => l.Id).ToList();
            var linkEntityIds = orderedLinks.Where(l => l.LinkEntity != null).Select(l => l.LinkEntity.Id);
            var linkEntities = GetEntitiesById(linkEntityIds);

            var relatedEntities = !string.IsNullOrWhiteSpace(config.TargetIdentifierField) ? GetEntitiesById(orderedLinks.Select(l => isInbound ? l.Source.Id : l.Target.Id)) : new Dictionary<int, Entity>();
            var results = new List<Dictionary<string, object>>();

            foreach (var link in orderedLinks)
            {
                if (link.LinkEntity == null)
                    continue;

                if (!linkEntities.TryGetValue(link.LinkEntity.Id, out var linkEntity))
                    continue;

                var result = new Dictionary<string, object>();

                var relatedEntityId = isInbound ? link.Source.Id : link.Target.Id;

                if (!string.IsNullOrWhiteSpace(config.TargetIdentifierField))
                {
                    if (!relatedEntities.TryGetValue(relatedEntityId, out var relatedEntity))
                        continue;

                    var identifier = relatedEntity.GetField(config.TargetIdentifierField)?.Data;
                    var identifierPropertyName = config.TargetIdentifierField;

                    if (config.FieldAliases != null && config.FieldAliases.TryGetValue(config.TargetIdentifierField, out var identifierAlias) && !string.IsNullOrWhiteSpace(identifierAlias))
                    {
                        identifierPropertyName = identifierAlias;
                    }

                    result[identifierPropertyName] = identifier?.ToString();
                }

                var fields = linkEntity.Fields.OrderBy(f => f.FieldType.Id).AsEnumerable();

                if (config.Fields != null && config.Fields.Any())
                {
                    var selectedFields = new HashSet<string>(config.Fields, StringComparer.OrdinalIgnoreCase);
                    fields = fields.Where(f => selectedFields.Contains(f.FieldType.Id));
                }

                foreach (var field in fields)
                {
                    var fieldId = field.FieldType.Id;
                    var propertyName = fieldId;

                    if (config.FieldAliases != null && config.FieldAliases.TryGetValue(fieldId, out var alias) && !string.IsNullOrWhiteSpace(alias))
                    {
                        propertyName = alias;
                    }

                    result[propertyName] = ConvertFieldValue(field);
                }

                results.Add(result);
            }

            var json = JsonConvert.SerializeObject(results);

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = json
            });
        }

        private object ConvertFieldValue(Field field)
        {
            if (field.Data == null)
                return null;

            if (field.FieldType.DataType == DataType.LocaleString)
            {
                var localeString = field.Data as LocaleString;

                if (localeString == null)
                    return null;

                return localeString.Languages.OrderBy(culture => culture.Name).ToDictionary(culture => culture.Name, culture => localeString[culture]);
            }

            if (field.FieldType.DataType == DataType.CVL)
            {
                var cvlKeys = field.Data.ToString().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                var values = new List<object>();

                foreach (var key in cvlKeys)
                {
                    var cvlValue = _context.ExtensionManager.ModelService.GetCVLValueByKey(key, field.FieldType.CVLId);

                    if (cvlValue?.Value == null)
                        continue;

                    if (cvlValue.Value is LocaleString ls)
                    {
                        var localeValues = ls.Languages.OrderBy(culture => culture.Name).Where(culture => !string.IsNullOrEmpty(ls[culture]))
                            .ToDictionary(
                                culture => culture.Name,
                                culture => ls[culture]);

                        values.Add(localeValues);
                    }
                    else
                    {
                        values.Add(cvlValue.Value.ToString());
                    }
                }

                return field.FieldType.Multivalue
                    ? (object)values
                    : values.FirstOrDefault();
            }

            return JToken.FromObject(field.Data);
        }

        private Dictionary<int, Entity> GetEntitiesById(IEnumerable<int> entityIds)
        {
            var dataService = _context.ExtensionManager.DataService;

            var ids = entityIds.Distinct().ToList();

            var result = new Dictionary<int, Entity>();

            foreach (var batch in ids.Batch(500))
            {
                var entities = dataService.GetEntities(batch.ToList(), LoadLevel.DataOnly);

                foreach (var entity in entities)
                {
                    if (entity != null)
                        result[entity.Id] = entity;
                }
            }

            return result;
        }
    }
}