using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Infrastructure.Extensions;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Enums;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class LinkedEntityFieldValuesExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public LinkedEntityFieldValuesExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var (fieldIds, sort, indexes) = ParseParams(settings.Params);
            if (fieldIds == null || indexes == null) return;

            var linkedEntityIds = GetLinkedEntityIds(inRiverEntity, settings);

            if (!linkedEntityIds.Any())
            {
                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = string.Empty,
                    Value = string.Empty
                });
                return;
            };

            var values = new List<string>();
            foreach (var linkedEntity in GetLinkedEntities(linkedEntityIds))
            {
                values.Add(string.Join(":", fieldIds.Select((t, i) => GetFieldValue(linkedEntity, t, indexes[i]))));
            }

            if (values.Any())
            {
                if (sort)
                {
                    values.Sort();
                }

                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = string.Empty,
                    Value = string.Join(Constants.Occtoo.MultiValueDefaultSeparator, values)
                });
                return;
            }

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = string.Empty
            });
        }

        private List<int> GetLinkedEntityIds(Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            List<int> linkedEntityIds;
            switch (settings.Type)
            {
                case ExceptionFieldType.ChildrenFieldValues:
                    linkedEntityIds = _context.ExtensionManager.DataService
                        .GetOutboundLinksForEntityAndLinkType(inRiverEntity.Id, settings.Id)
                        .Select(x => x.Target.Id).ToList();
                    break;

                case ExceptionFieldType.ParentFieldValues:
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

        private static string GetFieldValue(Entity entity, string fieldId, int index)
        {
            try
            {
                var entityValue = entity.GetField(fieldId)?.Data?.ToString() ?? string.Empty;
                if (string.IsNullOrEmpty(entityValue)) return string.Empty;

                if (index == 0) return entityValue;

                if (index > 0)
                {
                    return index >= entityValue.Length
                        ? entityValue
                        : entityValue.Substring(index);
                }

                return index + entityValue.Length <= 0
                    ? entityValue
                    : entityValue.Substring(entityValue.Length + index);
            }
            catch
            {
                return string.Empty;
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

        private static (List<string> fieldIds, bool sort, List<int> indexes) ParseParams(string settingsParams)
        {
            if (string.IsNullOrEmpty(settingsParams)) return (null, false, null);

            var paramsParts = settingsParams.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

            if (paramsParts.Length != 3) return (null, false, null);

            var fields = paramsParts[0].Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var indexesStr = paramsParts[2].Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var indexes = new List<int>();
            foreach (var indexStr in indexesStr)
            {
                if (int.TryParse(indexStr, out var index))
                {
                    indexes.Add(index);
                }
            }

            if (fields.Count == 0 || indexes.Count == 0 || fields.Count != indexes.Count) return (null, false, null);

            var sort = false;
            if (bool.TryParse(paramsParts[1], out var sorting))
            {
                sort = sorting;
            }

            return (fields, sort, indexes);
        }
    }
}