using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Infrastructure.Extensions;
using Occtoo.Generic.Inriver.Model;
using Occtoo.Generic.Inriver.Model.Enums;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using EntityType = Occtoo.Generic.Inriver.Model.Enums.EntityType;

namespace Occtoo.Generic.Inriver.Services
{
    public interface IExporterService
    {
        List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> EntityChanged(Entity entity, int initiatorId, IEnumerable<string> fields, bool deleted = false);
        List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> LinkChanged(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId);
        void FullExport();
    }

    public class ExporterService : IExporterService
    {
        private const int DocumentsBatchSize = 500;

        private readonly inRiverContext _context;
        private readonly Settings _settings;
        private readonly IEntitiesService _entitiesService;
        private readonly IDocumentsService _documentsService;

        public ExporterService(inRiverContext context,
            Settings settings,
            IEntitiesService entitiesService,
            IDocumentsService documentsService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _entitiesService = entitiesService ?? throw new ArgumentNullException(nameof(entitiesService));
            _documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
        }

        public List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> EntityChanged(Entity entity, int initiatorId, IEnumerable<string> fields, bool deleted = false)
        {
            var results = new List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)>();
            var entitySettings = _entitiesService.ReadEntitySettings(entity, fields);
            if (!entitySettings.Any()) return results;

            if (!deleted)
            {
                foreach (var entSettings in entitySettings.Where(x => x.Type != EntityType.Sku && x.UpdateParentsOnChange))
                {
                    foreach (var parMerges in entSettings.ParentsMerges.Where(x => x.Type == MergeType.None))
                    {
                        var parentLinks =
                            _context.ExtensionManager.DataService.GetInboundLinksForEntityAndLinkType(entity.Id,
                                parMerges.Link);
                        var parents =
                            _context.ExtensionManager.DataService.GetEntities(
                                parentLinks.Select(x => x.Source.Id).ToList(), LoadLevel.DataOnly);
                        foreach (var parent in parents)
                        {
                            results.AddRange(EntityChanged(parent, parent.Id, new List<string>()));
                        }
                    }
                }
            }

            foreach (var settings in entitySettings.Where(x => x.Type == EntityType.Sku))
            {
                if (SkuSettingsValid(entity.Id, settings))
                {
                    results.AddRange(_entitiesService.UpdateSku(entity, settings, deleted));
                }
            }

            var entityParentsList = GetEntityParents(entity, entitySettings.Where(x => x.Type != EntityType.Sku), deleted);
            foreach (var entityParents in entityParentsList)
            {
                results.AddRange(_entitiesService.UpdateEntity(entityParents, entitySettings.Where(x => x.DataSource == entityParents.DataSource).ToList(), deleted));
            }

            return results;
        }

        public List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> LinkChanged(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            var results = new List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)>();
            ReadLinkSettings(linkTypeId, out var updateSource, out var updateTarget);

            if (updateSource)
            {
                var entity = _context.ExtensionManager.DataService.GetEntity(sourceId, LoadLevel.DataOnly);
                if (entity == null)
                {
                    _context.Log(LogLevel.Information, $"LinkChange handling hickup entity {sourceId} doesn't exist, event skipped");
                    return results;
                }

                results.AddRange(EntityChanged(entity, entity.Id, new List<string>()));
            }

            if (updateTarget)
            {
                var entity = _context.ExtensionManager.DataService.GetEntity(targetId, LoadLevel.DataOnly);
                if(entity == null)
                {
                    _context.Log(LogLevel.Information, $"LinkChange handling hickup entity {targetId} doesn't exist, event skipped");
                    return results;
                }

                results.AddRange(EntityChanged(entity, entity.Id, new List<string>()));
            }

            return results;
        }

        public void FullExport()
        {
            // Documents waiting to be sent, per data source, for the whole run
            var dataSources = new Dictionary<string, DataSourceDocuments>();

            var mediaEntityTypes = _settings.ExportSettings.Entities
                .Where(x => x.ParentsMerges.All(m => m.Type != MergeType.Full) && x.Type == EntityType.Media && !x.SkipInFullExport)
                .Select(x => x.Name).Distinct();
            ExportEntityTypes(mediaEntityTypes, dataSources);

            var skuEntityTypes = _settings.ExportSettings.Entities.Where(x => x.Type == EntityType.Sku && !x.SkipInFullExport)
                .Select(x => x.Name).Distinct();
            ExportEntityTypes(skuEntityTypes, dataSources);

            var baseEntities = _settings.ExportSettings.Entities
                .Where(x => x.ParentsMerges.All(m => m.Type != MergeType.Full) && x.Type == EntityType.Entity && !x.SkipInFullExport)
                .Select(x => x.Name).Distinct();
            ExportEntityTypes(baseEntities, dataSources);

            foreach (var dataSource in dataSources)
            {
                _context.Log(LogLevel.Information, $"Occtoo full export - data source: {dataSource.Key} - sent documents: {dataSource.Value.Sent} - failed documents: {dataSource.Value.Failed} - skipped repeated documents: {dataSource.Value.Skipped}");
            }
        }

        private void ExportEntityTypes(IEnumerable<string> entityTypes, Dictionary<string, DataSourceDocuments> dataSources)
        {
            foreach (var entityType in entityTypes)
            {
                var entities = GetEntities(entityType);
                foreach (var entity in entities)
                {
                    try
                    {
                        _context.Log(LogLevel.Information, $"Occtoo full export - entity type: {entityType} - id: {entity.Id}");
                        var documents = EntityChanged(entity, entity.Id, new List<string>());
                        AddDocuments(documents, dataSources);
                    }
                    catch (Exception ex)
                    {
                        _context.Log(LogLevel.Error, $"Occtoo - Full Export - Error while exporting entity: {entity.Id}", ex);
                    }
                }

                foreach (var dataSource in dataSources.Where(x => x.Value.Documents.Any()))
                {
                    SendDocuments(dataSource.Key, dataSource.Value);
                }
            }
        }

        private void AddDocuments(List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> documents, Dictionary<string, DataSourceDocuments> dataSources)
        {
            foreach (var document in documents)
            {
                if (!dataSources.TryGetValue(document.Type, out var dataSource))
                {
                    dataSource = new DataSourceDocuments();
                    dataSources.Add(document.Type, dataSource);
                }

                // UpdateParentsOnChange builds the same parent document once for every child, send it only once
                if (!string.IsNullOrEmpty(document.Document.Key) && !dataSource.Keys.Add(document.Document.Key))
                {
                    dataSource.Skipped++;
                    continue;
                }

                if (!dataSource.Documents.Any())
                {
                    dataSource.EntitySystemIdAlias = document.EntitySystemIdAlias;
                }

                dataSource.Documents.Add(document.Document);
                if (dataSource.Documents.Count >= DocumentsBatchSize)
                {
                    SendDocuments(document.Type, dataSource);
                }
            }
        }

        private void SendDocuments(string dataSourceName, DataSourceDocuments dataSource)
        {
            var documents = dataSource.Documents.ToList();
            dataSource.Documents.Clear();

            try
            {
                var failedIds = _documentsService.SendDocuments(dataSourceName, documents, dataSource.EntitySystemIdAlias).ToList();
                if (failedIds.Any())
                {
                    dataSource.Failed += documents.Count;
                    _context.Log(LogLevel.Error, $"Occtoo - Full Export - Error while sending {documents.Count} documents to data source: {dataSourceName} - entity ids: {string.Join(", ", failedIds)}");
                    return;
                }

                dataSource.Sent += documents.Count;
            }
            catch (Exception ex)
            {
                dataSource.Failed += documents.Count;
                _context.Log(LogLevel.Error, $"Occtoo - Full Export - Error while sending {documents.Count} documents to data source: {dataSourceName}", ex);
            }
        }

        private List<Entity> GetEntities(string type)
        {
            var entities = new List<Entity>();

            var ids = _context.ExtensionManager.DataService.GetAllEntityIdsForEntityType(type).ToList();

            foreach (var batch in ids.Batch(1000))
            {
                entities.AddRange(_context.ExtensionManager.DataService.GetEntities(batch.ToList(), LoadLevel.DataOnly));
            }

            return entities;
        }

        private void ReadLinkSettings(string linkTypeId, out bool updateSource, out bool updateTarget)
        {
            updateSource = false;
            updateTarget = false;
            foreach (var entitySettings in _settings.ExportSettings.Entities)
            {
                foreach (var mergeSettings in entitySettings.ChildrenMerges.Where(x => x.Link == linkTypeId))
                {
                    switch (mergeSettings.LinkUpdateType)
                    {
                        case LinkUpdateType.UpdateSource:
                            updateSource = true;
                            break;
                        case LinkUpdateType.UpdateTarget:
                            updateTarget = true;
                            break;
                    }
                }

                foreach (var mergeSettings in entitySettings.ParentsMerges.Where(x => x.Link == linkTypeId))
                {
                    switch (mergeSettings.LinkUpdateType)
                    {
                        case LinkUpdateType.UpdateSource:
                            updateSource = true;
                            break;
                        case LinkUpdateType.UpdateTarget:
                            updateTarget = true;
                            break;
                    }
                }
            }
        }

        private bool SkuSettingsValid(int entityId, EntitySettings settings)
        {
            if (!settings.Fields.Any(x => x.Value.IsSku))
            {
                _context.Log(LogLevel.Error, $"Sku field is missing in settings for entity: {entityId}");
                return false;
            }

            if (settings.Fields.Count(x => x.Value.IsSku) > 1)
            {
                _context.Log(LogLevel.Error, $"More than one sku field in settings for entity: {entityId}");
                return false;
            }

            var fieldSettings = settings.Fields.First(x => x.Value.IsSku);

            if (fieldSettings.Value.SkuType == SkuType.None)
            {
                _context.Log(LogLevel.Error, $"Sku type is not defined in settings for entity: {entityId}");
                return false;
            }

            if (string.IsNullOrEmpty(fieldSettings.Value.PathToProps))
            {
                _context.Log(LogLevel.Error, $"Sku path to properties list is not defined for entity: {entityId}");
                return false;
            }

            if (!fieldSettings.Value.SkuIds.Any())
            {
                _context.Log(LogLevel.Error, $"Sku id fields were not defined in settings for entity: {entityId}");
                return false;
            }

            if (!settings.UniqueIdFields.Any())
            {
                _context.Log(LogLevel.Error, $"Entity unique id fields were not defined for entity: {entityId}");
                return false;
            }

            return true;
        }

        private List<EntityParents> GetEntityParents(Entity entity, IEnumerable<EntitySettings> entitySettings, bool delete)
        {
            var response = new List<EntityParents>();

            foreach (var settings in entitySettings)
            {
                if (settings.ParentsMerges.All(x => x.Type != MergeType.Full))
                {
                    var entityParents = new EntityParents
                    {
                        IsTreeCompleted = true,
                        DataSource = settings.DataSource,
                        Parents = new List<Entity> { entity },
                        Created = entity.DateCreated,
                        Modified = entity.LastModified
                    };
                    if (delete)
                    {
                        ExtractDeleteKeyProps(entity, settings.UniqueIdFields, entityParents.DeleteKeyProps);
                    }
                    response.Add(entityParents);
                    continue;
                }

                foreach (var parentMerge in settings.ParentsMerges)
                {
                    if (parentMerge.Type != MergeType.Full) continue;

                    if (parentMerge.DataSource != settings.DataSource)
                    {
                        _context.Log(LogLevel.Error, $"Occtoo settings error: entity '{settings.Name}' in data source '{settings.DataSource}' has a Full parents merge on link '{parentMerge.Link}' with DataSource '{parentMerge.DataSource}'. Full merges flatten into one data source: set the merge's DataSource to '{settings.DataSource}' and add a '{parentMerge.Name}' entity setting with DataSource '{settings.DataSource}'.");
                    }

                    var parents = _context.ExtensionManager.GetParentEntities(entity, parentMerge.Link);

                    if (delete && !parents.Any())
                    {
                        var entityParents = new EntityParents
                        {
                            IsTreeCompleted = true,
                            DataSource = settings.DataSource,
                            Parents = new List<Entity> { entity },
                            Created = entity.DateCreated,
                            Modified = entity.LastModified
                        };
                        ExtractDeleteKeyProps(entity, settings.UniqueIdFields, entityParents.DeleteKeyProps);
                        response.Add(entityParents);
                        continue;
                    }

                    foreach (var parent in parents)
                    {
                        var parentEntitySettings = _settings.ExportSettings.Entities.Where(x =>
                            x.Name == parent.EntityType.Id && x.DataSource == parentMerge.DataSource).ToList();

                        if (!parentEntitySettings.Any())
                        {
                            _context.Log(LogLevel.Error, $"Occtoo settings error: entity '{settings.Name}' in data source '{settings.DataSource}' has a Full parents merge on link '{parentMerge.Link}', but there is no '{parent.EntityType.Id}' entity setting with DataSource '{parentMerge.DataSource}'. Add that entity setting or change the merge's Type. Entity {entity.Id} was not exported through parent {parent.Id}.");
                            response.Add(new EntityParents { IsTreeCompleted = false });
                            continue;
                        }

                        var parentEntities = GetEntityParents(parent, parentEntitySettings, delete);
                        foreach (var pEntity in parentEntities)
                        {
                            pEntity.Parents.Add(entity);
                            if (entity.DateCreated < pEntity.Created)
                            {
                                pEntity.Created = entity.DateCreated;
                            }

                            if (entity.LastModified > pEntity.Modified)
                            {
                                pEntity.Modified = entity.LastModified;
                            }

                            if (delete)
                            {
                                ExtractDeleteKeyProps(entity, settings.UniqueIdFields, pEntity.DeleteKeyProps);
                            }
                        }
                        response.AddRange(parentEntities);
                    }
                }
            }

            return response;
        }

        private static void ExtractDeleteKeyProps(Entity entity, List<string> uniqueFields, Dictionary<string, string> deleteKeyProps)
        {
            foreach (var fieldId in uniqueFields)
            {
                var field = entity.GetField(fieldId);
                if (field == null || field.IsEmpty() || deleteKeyProps.ContainsKey(fieldId)) continue;
                deleteKeyProps.Add(fieldId, field.Data.ToString());
            }
        }

        private class DataSourceDocuments
        {
            // Documents waiting for the next batch
            public List<DynamicEntity> Documents { get; } = new List<DynamicEntity>();

            // Keys already sent or waiting to be sent during this full export
            public HashSet<string> Keys { get; } = new HashSet<string>(StringComparer.Ordinal);

            public string EntitySystemIdAlias { get; set; }

            public int Sent { get; set; }

            public int Failed { get; set; }

            public int Skipped { get; set; }
        }
    }
}