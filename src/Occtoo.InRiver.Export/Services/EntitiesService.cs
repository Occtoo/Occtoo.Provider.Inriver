using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Occtoo.Generic.Infrastructure.Extensions;
using Occtoo.Generic.Inriver.Extractors.Factory;
using Occtoo.Generic.Inriver.Helpers;
using Occtoo.Generic.Inriver.Model;
using Occtoo.Generic.Inriver.Model.Enums;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using EntityType = Occtoo.Generic.Inriver.Model.Enums.EntityType;

namespace Occtoo.Generic.Inriver.Services
{
    public interface IEntitiesService
    {
        List<EntitySettings> ReadEntitySettings(Entity entity, IEnumerable<string> fields);

        List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> UpdateEntity(EntityParents entityParents, List<EntitySettings> entitySettings, bool deleted);

        List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> UpdateSku(Entity entity, EntitySettings settings, bool deleted);
    }

    public class EntitiesService : IEntitiesService
    {
        private readonly inRiverContext _context;
        private readonly IDocumentsService _documentsService;
        private readonly IMediaService _mediaService;
        private readonly Settings _settings;
        private readonly IExtractorsFactory _extractorsFactory;

        public EntitiesService(inRiverContext context,
            Settings settings,
            IDocumentsService documentsService,
            IMediaService mediaService,
            IExtractorsFactory extractorsFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
            _extractorsFactory = extractorsFactory ?? throw new ArgumentNullException(nameof(extractorsFactory));
        }

        public List<EntitySettings> ReadEntitySettings(Entity entity, IEnumerable<string> fields)
        {
            var entitySettings = _settings.ExportSettings.Entities.Where(x => x.Name == entity.EntityType.Id).ToList().ToList();

            if (fields != null)
            {
                entitySettings = entitySettings.Where(settings => !FieldFiltered(fields.ToList(), settings)).ToList();
            }

            return entitySettings;
        }

        public List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> UpdateEntity(EntityParents entityParents, List<EntitySettings> entitySettings, bool deleted)
        {
            var result = new List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)>();
            foreach (var settings in entitySettings)
            {
                var entity = entityParents.Parents.Last();

                if (deleted)
                {
                    _context.Log(LogLevel.Debug, $"Deleting entity with id: {entity.Id}");
                    var deleteId = GetDocumentId(entity, settings, entityParents.DeleteKeyProps);
                    _context.Log(LogLevel.Debug, $"Deleting entity with entity id: {entity.Id} and occtoo id: {deleteId}");
                    var deleteDocuments = new List<DynamicEntity>
                    {
                        new DynamicEntity
                        {
                            Delete = true,
                            Key = deleteId,
                            Properties = new List<DynamicProperty>
                            {
                                new DynamicProperty { Id = settings.EntityIdAlias, Value = entity.Id.ToString(), Language = string.Empty }
                            }
                        }
                    };
                    if (!string.IsNullOrEmpty(deleteId))
                    {
                        if (settings.Type == EntityType.Media)
                        {
                            _mediaService.SendDocuments(entity, settings, deleteDocuments, settings.EntityIdAlias);
                        }
                        else
                        {
                            result.Add((deleteDocuments.First(), settings.DataSource, settings.EntityIdAlias));
                        }
                        continue;
                    }
                }

                var baseDocument = GetBaseDocument(entityParents, settings);
                if (baseDocument == null) continue;

                var entityParentsCreated = entityParents.Created;
                var entityParentsModified = entityParents.Modified;
                var dynamicEntitiesList = GetChildrenDocuments(entity, baseDocument, settings, entityParents.DataSource, ref entityParentsCreated, ref entityParentsModified);

                MergePartialEntities(entity, settings, dynamicEntitiesList);

                var documents = new List<DynamicEntity>();
                foreach (var dynamicEntity in dynamicEntitiesList)
                {
                    var id = GetDocumentId(entity, settings, dynamicEntity);
                    if (string.IsNullOrEmpty(id)) continue;

                    dynamicEntity.Key = id;
                    dynamicEntity.Properties.Add(ValueHelpers.GetValue("Modified", entityParentsModified, string.Empty));
                    dynamicEntity.Properties.Add(ValueHelpers.GetValue("Created", entityParentsCreated, string.Empty));
                    dynamicEntity.Properties.Add(ValueHelpers.GetValue("Segment", JsonConvert.SerializeObject(new[] { "AllContent" }), string.Empty));
                    var filtered = Filtered(dynamicEntity, settings);

                    if (_settings.SkipFilteredEntities && filtered)
                    {
                        continue;
                    }

                    dynamicEntity.Delete = deleted || filtered;
                    documents.Add(dynamicEntity);
                }

                if (!documents.Any()) continue;

                if (settings.Type == EntityType.Media)
                {
                    _mediaService.SendDocuments(entity, settings, documents, settings.EntityIdAlias);
                }
                else
                {
                    foreach (var document in documents)
                    {
                        result.Add((document, settings.DataSource, settings.EntityIdAlias));
                    }
                }
            }

            return result;
        }

        public List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> UpdateSku(Entity entity, EntitySettings settings, bool deleted)
        {
            var result = new List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)>();
            var skuFieldSettings = settings.Fields.First(x => x.Value.IsSku);
            var skus = ExtractSkusData(entity, skuFieldSettings);

            if (skus == null || !skus.Any()) return result;

            var baseDoc = new DynamicEntity();
            foreach (var field in settings.Fields)
            {
                var id = string.IsNullOrEmpty(field.Value.Alias) ? field.Key : field.Value.Alias;
                baseDoc.Properties.Add(ValueHelpers.GetValue(id, entity.GetField(field.Key)?.Data, ""));
            }

            var baseId = GetSkuBaseId(entity, settings);

            var documents = new List<DynamicEntity>();
            foreach (var sku in skus)
            {
                var docId = GetSkuId(baseId, sku, skuFieldSettings.Value);
                if (string.IsNullOrEmpty(docId)) continue;

                var dynEntity = new DynamicEntity { Key = docId };

                foreach (var s in sku)
                {
                    dynEntity.Properties.Add(new DynamicProperty { Id = s.Key, Language = "", Value = s.Value });
                }

                dynEntity.Properties.Add(ValueHelpers.GetValue("EntityId", entity.Id, string.Empty));
                dynEntity.Properties.Add(ValueHelpers.GetValue("Modified", entity.LastModified, string.Empty));
                dynEntity.Properties.Add(ValueHelpers.GetValue("Created", entity.DateCreated, string.Empty));
                dynEntity.Properties.Add(ValueHelpers.GetValue("Segment", JsonConvert.SerializeObject(new[] { "AllContent" }), string.Empty));

                var filtered = Filtered(dynEntity, settings);

                if (_settings.SkipFilteredEntities && filtered)
                {
                    continue;
                }

                dynEntity.Delete = deleted || filtered;
                documents.Add(dynEntity);
            }

            if (documents.Any())
            {
                foreach (var document in documents)
                {
                    result.Add((document, settings.DataSource, settings.EntityIdAlias));
                }
            }

            return result;
        }

        private string GetSkuId(string baseId, Dictionary<string, string> sku, FieldSettings settings)
        {
            var newId = baseId;
            foreach (var propId in settings.SkuIds)
            {
                if (!sku.ContainsKey(propId))
                {
                    _context.Log(LogLevel.Error, $"Sku is missing property which is part of id: {propId}");
                    return null;
                }

                if (newId.Length > 0)
                {
                    newId += "_";
                }

                newId += sku[propId];
            }

            return newId;
        }

        private List<Dictionary<string, string>> ExtractSkusData(Entity entity, KeyValuePair<string, FieldSettings> settings)
        {
            return settings.Value.SkuType == SkuType.Json ? ExtractXmlSkusData(entity, settings.Key, settings.Value.PathToProps) : ExtractJsonSkusData(entity, settings.Key, settings.Value.PathToProps);
        }

        private List<Dictionary<string, string>> ExtractJsonSkusData(Entity entity, string fieldId, string path)
        {
            try
            {
                var fieldData = entity.GetData<string>(fieldId);
                if (string.IsNullOrEmpty(fieldData)) return null;

                var jObject = JObject.Parse(fieldData);
                var jTokens = jObject.SelectTokens(path).ToList();

                if (!jTokens.Any()) return null;

                var response = new List<Dictionary<string, string>>();

                foreach (var jToken in jTokens)
                {
                    var skuDict = new Dictionary<string, string>();
                    foreach (var child in jToken.Children())
                    {
                        if (!(child is JProperty jProp)) continue;

                        if (!skuDict.ContainsKey(jProp.Name))
                        {
                            skuDict.Add(jProp.Name, jProp.Value.ToString());
                        }
                    }
                    response.Add(skuDict);
                }

                return response;
            }
            catch (Exception ex)
            {
                _context.Log(LogLevel.Error,
                    $"Could not extract sku data for entity: {entity.Id} - path: {path}", ex);
                return null;
            }
        }

        private List<Dictionary<string, string>> ExtractXmlSkusData(Entity entity, string fieldId, string path)
        {
            try
            {
                var fieldData = entity.GetData<string>(fieldId);
                if (string.IsNullOrEmpty(fieldData)) return null;

                var xDoc = XDocument.Parse(fieldData);
                var elements = xDoc.XPathSelectElements(path).ToList();
                if (!elements.Any()) return null;

                var response = new List<Dictionary<string, string>>();

                foreach (var element in elements)
                {
                    var skuDictionary = new Dictionary<string, string>();
                    foreach (var xAttribute in element.Attributes())
                    {
                        if (!skuDictionary.ContainsKey(xAttribute.Name.LocalName))
                        {
                            skuDictionary.Add(xAttribute.Name.LocalName, xAttribute.Value);
                        }
                    }

                    foreach (var el in element.Descendants())
                    {
                        if (!skuDictionary.ContainsKey(el.Name.LocalName))
                        {
                            skuDictionary.Add(el.Name.LocalName, el.Value);
                        }
                    }
                    response.Add(skuDictionary);
                }

                return response;
            }
            catch (Exception ex)
            {
                _context.Log(LogLevel.Error,
                    $"Could not extract sku data for entity: {entity.Id} - path: {path}", ex);
                return null;
            }
        }

        private static bool FilterFieldDoesNotContain(FilterSettings filter, DynamicEntity dynamicEntity)
        {
            var property = dynamicEntity.Properties.FirstOrDefault(x =>
                x.Id == filter.Field && x.Language == filter.Language);
            if (property == null || string.IsNullOrEmpty(property.Value)) return false;

            var propValues = property.Value.Split(new[] { Constants.Occtoo.MultiValueDefaultSeparator },
                StringSplitOptions.RemoveEmptyEntries);

            return !filter.Values.Any(v => propValues.Contains(v));
        }

        private bool FilterAny(string fieldValue, IEnumerable<string> filterValues)
        {
            var fieldValues = fieldValue.Split(new[] { Constants.InRiver.MultiValueSeparator }, StringSplitOptions.RemoveEmptyEntries);

            return filterValues.Any(value => fieldValues.Contains(value));
        }

        private bool FilterFieldEqual(FilterSettings filter, DynamicEntity dynamicEntity, string fieldValue)
        {
            var property = dynamicEntity.Properties.FirstOrDefault(x =>
                x.Id == filter.Values.FirstOrDefault() && x.Language == filter.Language);
            if (property == null)
                return false;

            var needle = property.Value;
            if (string.IsNullOrEmpty(fieldValue) || string.IsNullOrEmpty(needle))
            {
                return false;
            }

            return fieldValue == needle;
        }

        private bool FilterFieldNotEqual(FilterSettings filter, DynamicEntity dynamicEntity, string fieldValue)
        {
            var property = dynamicEntity.Properties.FirstOrDefault(x =>
                x.Id == filter.Values.FirstOrDefault() && x.Language == filter.Language);
            if (property == null)
                return false;

            var needle = property.Value;
            if (string.IsNullOrEmpty(fieldValue) || string.IsNullOrEmpty(needle))
            {
                return false;
            }

            return fieldValue != needle;
        }

        private bool Filtered(DynamicEntity dynamicEntity, EntitySettings settings)
        {
            var response = false;

            foreach (var filter in settings.Filters)
            {
                var property = dynamicEntity.Properties.FirstOrDefault(x => x.Id == filter.Field && x.Language == filter.Language);
                if (property == null)
                {
                    if (filter.Type == FilterType.Null)
                    {
                        return true;
                    }
                    continue;
                }

                var fieldValue = property.Value;
                switch (filter.Type)
                {
                    case FilterType.Equal:
                        response = fieldValue == filter.Values[0];
                        break;

                    case FilterType.All:
                        response = fieldValue == string.Join(";", filter.Values);
                        break;

                    case FilterType.Any:
                        response = FilterAny(fieldValue, filter.Values);
                        break;

                    case FilterType.Contains:
                        response = filter.Values.Contains(fieldValue);
                        break;

                    case FilterType.FieldEqual:
                        response = FilterFieldEqual(filter, dynamicEntity, fieldValue);
                        break;

                    case FilterType.FieldNotEqual:
                        response = FilterFieldNotEqual(filter, dynamicEntity, fieldValue);
                        break;

                    case FilterType.Empty:
                        response = string.IsNullOrEmpty(fieldValue);
                        break;

                    case FilterType.DoesNotContain:
                        response = FilterFieldDoesNotContain(filter, dynamicEntity);
                        break;
                }

                if (response)
                {
                    break;
                }
            }

            return response;
        }

        private static bool FieldFiltered(IReadOnlyCollection<string> fields, EntitySettings settings)
        {
            if (!fields.Any()) return false;

            var response = false;

            foreach (var filter in settings.FieldFilterSettings)
            {
                switch (filter.Type)
                {
                    case FieldFilterType.Equal:
                        response = filter.Rule == FilterApplyRule.All
                            ? fields.All(x => x == filter.Value)
                            : fields.Any(x => x == filter.Value);
                        break;

                    case FieldFilterType.StartsWith:
                        response = filter.Rule == FilterApplyRule.All
                            ? fields.All(x => x.StartsWith(filter.Value))
                            : fields.Any(x => x.StartsWith(filter.Value));
                        break;

                    case FieldFilterType.EndsWith:
                        response = filter.Rule == FilterApplyRule.All
                            ? fields.All(x => x.EndsWith(filter.Value))
                            : fields.Any(x => x.EndsWith(filter.Value));
                        break;

                    case FieldFilterType.Contains:
                        response = filter.Rule == FilterApplyRule.All
                            ? fields.All(x => x.Contains(filter.Value))
                            : fields.Any(x => x.Contains(filter.Value));
                        break;
                }

                if (response)
                {
                    break;
                }
            }

            return response;
        }

        private DynamicEntity GetBaseDocument(EntityParents entityParents, EntitySettings settings)
        {
            var response = new DynamicEntity();

            foreach (var entity in entityParents.Parents)
            {
                var entitySettings = _settings.ExportSettings.Entities.FirstOrDefault(x => x.Name == entity.EntityType.Id && x.DataSource == settings.DataSource);
                if (entitySettings == null) return null;

                response.Properties.AddRange(GetValues(entity, entitySettings));

                ExtractExceptionalFields(entitySettings, response, entity);

                MergePartialEntities(entity, entitySettings, new List<DynamicEntity> { response });
            }

            return response;
        }

        private void ExtractExceptionalFields(EntitySettings entitySettings, DynamicEntity response, Entity entity)
        {
            foreach (var settings in entitySettings.ExceptionFields)
            {
                var extractor = _extractorsFactory.CreateExtractor(settings.Type, entitySettings.UniqueIdFields);
                extractor?.Extract(response, entity, settings);
            }
        }

        private List<DynamicEntity> GetChildrenDocuments(Entity entity, DynamicEntity baseDocument, EntitySettings settings, string dataSource, ref DateTime created, ref DateTime modified)
        {
            var response = new List<DynamicEntity>();

            if (settings.ChildrenMerges.Any(x => x.Type == MergeType.Full))
            {
                foreach (var childMerge in settings.ChildrenMerges.Where(x => x.Type == MergeType.Full))
                {
                    var children = _context.ExtensionManager.GetChildEntities(entity, childMerge.Link);
                    foreach (var child in children)
                    {
                        var childSettings = _settings.ExportSettings.Entities.First(x =>
                            x.Name == child.EntityType.Id && x.DataSource == dataSource);
                        var childResponse = GetChildrenDocuments(child, baseDocument, childSettings, dataSource, ref created, ref modified);

                        var childrenList = new List<DynamicEntity>();
                        foreach (var childRes in childResponse)
                        {
                            var de = new DynamicEntity();
                            de.Properties.AddRange(childRes.Properties);
                            de.Properties.AddRange(GetValues(child, childSettings));

                            if (!string.IsNullOrEmpty(childSettings.EntityIdAlias) && de.Properties.All(x => x.Id != childSettings.EntityIdAlias))
                            {
                                de.Properties.Add(new DynamicProperty { Id = childSettings.EntityIdAlias, Value = child.Id.ToString(), Language = string.Empty });
                            }

                            ExtractExceptionalFields(childSettings, de, child);

                            childrenList.Add(de);
                        }

                        MergePartialEntities(child, childSettings, childrenList);

                        response.AddRange(childrenList);

                        if (child.DateCreated < created)
                        {
                            created = child.DateCreated;
                        }

                        if (child.LastModified > modified)
                        {
                            modified = child.LastModified;
                        }
                    }
                }
            }
            else
            {
                response.Add(new DynamicEntity
                {
                    Properties = baseDocument.Properties
                });
            }

            return response;
        }

        private (string valueAsString, Dictionary<string, string> localeValues) GetCvlValues(Field field)
        {
            var valueAsString = string.Empty;
            var localeValues = new Dictionary<string, string>();

            if (field.FieldType.DataType != "CVL" || string.IsNullOrEmpty(field.FieldType.CVLId))
            {
                _context.Logger.Log(LogLevel.Warning, $"Field: {field.FieldType.Id} - is expected to be CVL but it is not or missing some configuration!");
                return (valueAsString, localeValues);
            }

            var cvlKeys = field.Data?.ToString().Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            foreach (var key in cvlKeys)
            {
                var value = _context.ExtensionManager.ModelService.GetCVLValueByKey(key, field.FieldType.CVLId);

                switch (value?.Value)
                {
                    case null:
                        continue;
                    case LocaleString ls:
                        {
                            foreach (var ci in ls.Languages)
                            {
                                var localeValue = ls[ci];

                                if (string.IsNullOrEmpty(localeValue))
                                {
                                    continue;
                                }

                                if (!localeValues.ContainsKey(ci.Name))
                                {
                                    localeValues.Add(ci.Name, localeValue);
                                }
                                else
                                {
                                    localeValues[ci.Name] = localeValues[ci.Name] + Constants.Occtoo.MultiValueDefaultSeparator + localeValue;
                                }
                            }

                            break;
                        }
                    default:
                        {
                            if (string.IsNullOrEmpty(valueAsString))
                            {
                                valueAsString = value.Value.ToString();
                            }
                            else
                            {
                                valueAsString = valueAsString + Constants.Occtoo.MultiValueDefaultSeparator + value.Value;
                            }

                            break;
                        }
                }
            }

            return (valueAsString, localeValues);
        }

        private string GetSkuBaseId(Entity entity, EntitySettings settings)
        {
            var idParts = new List<string>();
            foreach (var fieldId in settings.UniqueIdFields)
            {
                var idPart = entity.GetData<string>(fieldId);
                if (string.IsNullOrEmpty(idPart)) return string.Empty;

                idParts.Add(idPart);
            }

            var id = string.Join("_", idParts);
            foreach (var forbiddenChar in GetForbiddenChars())
            {
                id = id.Replace(forbiddenChar.Key, forbiddenChar.Value);
            }

            return id;
        }

        private string GetDocumentId(Entity entity, EntitySettings settings, DynamicEntity dynamicEntity)
        {
            if (!settings.UniqueIdFields.Any()) return string.Empty;

            var id = settings.Type == EntityType.Media
                ? ValueHelpers.GetMediaId(entity, settings.UniqueIdFields, GetMediaServiceNamespace())
                : GetEntityId(settings, dynamicEntity);

            foreach (var forbiddenChar in GetForbiddenChars())
            {
                id = id.Replace(forbiddenChar.Key, forbiddenChar.Value);
            }

            return id;
        }

        private string GetDocumentId(Entity entity, EntitySettings settings, Dictionary<string, string> uniqueFields)
        {
            if (!settings.UniqueIdFields.Any()) return string.Empty;

            var id = settings.Type == EntityType.Media
                ? ValueHelpers.GetMediaId(entity, settings.UniqueIdFields, GetMediaServiceNamespace())
                : GetEntityId(settings, uniqueFields);

            foreach (var forbiddenChar in GetForbiddenChars())
            {
                id = id.Replace(forbiddenChar.Key, forbiddenChar.Value);
            }

            return id;
        }

        private Dictionary<string, string> GetForbiddenChars()
        {
            var response = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(_settings.DocumentIdForbiddenChars)) return response;

            var charAndReplace = _settings.DocumentIdForbiddenChars.Split(new[] { '|' },
                StringSplitOptions.RemoveEmptyEntries);

            foreach (var cr in charAndReplace)
            {
                var chars = cr.Split(new[] { ';' }, StringSplitOptions.None);
                if (chars.Length == 2)
                {
                    response.Add(chars[0], chars[1]);
                }
            }

            return response;
        }

        private string GetEntityId(EntitySettings settings, DynamicEntity dynamicEntity)
        {
            var idParts = new List<string>();

            foreach (var fieldId in settings.UniqueIdFields)
            {
                var idPart = GetIdPart(fieldId, settings, dynamicEntity);
                if (string.IsNullOrEmpty(idPart)) return string.Empty;

                idParts.Add(idPart);
            }

            return string.Join("_", idParts);
        }

        private string GetEntityId(EntitySettings settings, Dictionary<string, string> uniqueFields)
        {
            var idParts = new List<string>();

            foreach (var fieldId in settings.UniqueIdFields)
            {
                var idPart = GetIdPart(fieldId, settings, uniqueFields);
                if (string.IsNullOrEmpty(idPart)) return string.Empty;

                idParts.Add(idPart);
            }

            return string.Join("_", idParts);
        }

        private string GetIdPart(string fieldId, EntitySettings settings, DynamicEntity dynamicEntity)
        {
            var propId = fieldId;
            if (settings.Fields.ContainsKey(fieldId))
            {
                if (!string.IsNullOrEmpty(settings.Fields[fieldId].Alias))
                {
                    propId = settings.Fields[fieldId].Alias;
                }
            }

            return dynamicEntity.Properties.FirstOrDefault(x => x.Id == propId)?.Value;
        }

        private string GetIdPart(string fieldId, EntitySettings settings, Dictionary<string, string> uniqueFields)
        {
            var propId = fieldId;
            if (settings.Fields.ContainsKey(fieldId))
            {
                if (!string.IsNullOrEmpty(settings.Fields[fieldId].Alias))
                {
                    propId = settings.Fields[fieldId].Alias;
                }
            }

            return uniqueFields.ContainsKey(propId)
                ? uniqueFields[propId]
                : null;
        }

        private string GetFieldAlias(EntitySettings settings, string fieldId)
        {
            var fieldSettingKey = settings.Fields.Keys.FirstOrDefault(x =>
            string.Equals(x, fieldId, StringComparison.InvariantCultureIgnoreCase));

            return string.IsNullOrEmpty(fieldSettingKey)
            ? fieldId
            : string.IsNullOrEmpty(settings.Fields[fieldSettingKey].Alias)
            ? fieldId
            : settings.Fields[fieldSettingKey].Alias;
        }

        private string GetMediaServiceNamespace()
        {
            return _settings.SkipMediaKeyGuidTransform
                ? _settings.Environment
                : Constants.Occtoo.DefaultMediaNamespace;
        }

        private DynamicProperty GetMergedIds(IEnumerable<Entity> relatives, MergeSettings settings)
        {
            var ids = new List<string>();

            var relativesSettings = _settings.ExportSettings.Entities.FirstOrDefault(x =>
                x.Name == settings.Name && x.DataSource == settings.DataSource);
            if (relativesSettings == null)
            {
                return new DynamicProperty
                {
                    Id = settings.PropertyAlias,
                    Language = string.Empty,
                    Value = string.Join(Constants.Occtoo.MultiValueDefaultSeparator, relatives.Select(x => x.Id))
                };
            }

            foreach (var entity in relatives)
            {
                var id = "";
                if (relativesSettings.Type == EntityType.Media)
                {
                    id = ValueHelpers.GetMediaId(entity, relativesSettings.UniqueIdFields, GetMediaServiceNamespace());
                }
                else
                {
                    foreach (var fieldId in relativesSettings.UniqueIdFields)
                    {
                        if (id.Length > 0)
                        {
                            id += "_";
                        }

                        if (fieldId == relativesSettings.EntityIdAlias)
                        {
                            id += entity.Id.ToString();
                        }
                        else
                        {
                            id += entity.GetData<string>(fieldId);
                        }
                    }
                }

                foreach (var forbiddenChar in GetForbiddenChars())
                {
                    id = id.Replace(forbiddenChar.Key, forbiddenChar.Value);
                }

                ids.Add(id);
            }

            return new DynamicProperty
            {
                Id = settings.PropertyAlias,
                Language = string.Empty,
                Value = string.Join(Constants.Occtoo.MultiValueDefaultSeparator, ids)
            };
        }

        private DynamicEntity GetPartiallyMergedEntities(Entity entity, EntitySettings settings)
        {
            var response = new DynamicEntity();
            foreach (var mergeSettings in settings.ChildrenMerges.Where(x => x.Type == MergeType.Ids))
            {
                var children = _context.ExtensionManager.GetChildEntities(entity, mergeSettings.Link);

                response.Properties.Add(GetMergedIds(children, mergeSettings));
            }

            foreach (var mergeSettings in settings.ParentsMerges.Where(x => x.Type == MergeType.ParentIds))
            {
                var parents = _context.ExtensionManager.GetParentEntities(entity, mergeSettings.Link);

                response.Properties.Add(GetMergedIds(parents, mergeSettings));
            }

            return response;
        }

        private List<DynamicProperty> GetValues(Entity entity, EntitySettings settings)
        {
            var values = new List<DynamicProperty>();

            if (entity == null)
            {
                return values;
            }

            var fields = entity.Fields.Where(field => (field.Data != null || field.Revision > 0))
                .Where(field => !settings.IgnoreList.Any(x => string.Equals(x, field.FieldType.Id, StringComparison.InvariantCultureIgnoreCase)));
            if (settings.Fields.Any())
            {
                fields = fields.Where(f => settings.Fields.Any(x => string.Equals(x.Key, f.FieldType.Id, StringComparison.InvariantCultureIgnoreCase)));
            }

            foreach (var field in fields)
            {
                if (field.FieldType.DataType == DataType.LocaleString)
                {
                    values.AddRange(field.Data == null
                        ? ValueHelpers.GetEmptyValuesForLocaleString(field, _context, GetFieldAlias(settings, field.FieldType.Id))
                        : ValueHelpers.GetValuesForLocaleStringFieldWithFallbackToDefault(field, _settings.DefaultLanguage, GetFieldAlias(settings, field.FieldType.Id)));

                    continue;
                }

                if (field.FieldType.DataType == DataType.CVL)
                {
                    if (string.IsNullOrEmpty(field.Data?.ToString()))
                    {
                        GetEmptyCvlValues(field, values);

                        continue;
                    }

                    var (valueAsString, localeValues) = GetCvlValues(field);

                    if (!string.IsNullOrEmpty(valueAsString))
                    {
                        values.Add(ValueHelpers.GetValue(GetFieldAlias(settings, field.FieldType.Id), valueAsString, string.Empty));
                    }

                    if (localeValues.Any())
                    {
                        values.AddRange(localeValues.Select(lsValue =>
                            ValueHelpers.GetValue(GetFieldAlias(settings, field.FieldType.Id), lsValue.Value, lsValue.Key)));
                    }

                    continue;
                }

                values.Add(ValueHelpers.GetValue(GetFieldAlias(settings, field.FieldType.Id), field.Data, string.Empty));
            }

            if (!string.IsNullOrEmpty(settings.EntityIdAlias))
            {
                //add entity inRiver id as property
                if (values.All(x => x.Id != settings.EntityIdAlias))
                {
                    values.Add(new DynamicProperty { Id = settings.EntityIdAlias, Value = entity.Id.ToString(), Language = string.Empty });
                }
            }

            return values;
        }

        private void GetEmptyCvlValues(Field field, List<DynamicProperty> values)
        {
            var cvl = _context.ExtensionManager.ModelService.GetCVL(field.FieldType.CVLId);
            if (cvl == null) return;

            if (cvl.DataType == "LocaleString")
            {
                var languages = _context.ExtensionManager.UtilityService.GetAllLanguages();
                values.AddRange(languages.Select(ci => ValueHelpers.GetValue(field.FieldType.Id, null, ci.Name)));
            }
            else
            {
                values.Add(ValueHelpers.GetValue(field.FieldType.Id, null, string.Empty));
            }
        }

        private void MergePartialEntities(Entity entity, EntitySettings settings, List<DynamicEntity> dynamicEntitiesList)
        {
            var mergedEntities = GetPartiallyMergedEntities(entity, settings);

            foreach (var dynamicEntity in dynamicEntitiesList)
            {
                foreach (var property in mergedEntities.Properties)
                {
                    var existingProperty = dynamicEntity.Properties.FirstOrDefault(x =>
                        x.Id == property.Id && x.Language == property.Language);

                    if (existingProperty == null)
                    {
                        dynamicEntity.Properties.Add(property);
                    }
                    else
                    {
                        var existingValues =
                            existingProperty.Value.Split(new[] { Constants.Occtoo.MultiValueDefaultSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        var newValues = property.Value.Split(new[] { Constants.Occtoo.MultiValueDefaultSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();
                        var nonExistingValues = newValues.Except(existingValues).ToList();
                        if (!nonExistingValues.Any()) continue;

                        existingValues.AddRange(nonExistingValues);
                        dynamicEntity.Properties.Remove(dynamicEntity.Properties.FirstOrDefault(x => x.Id == property.Id && x.Language == property.Language));
                        dynamicEntity.Properties.Add(new DynamicProperty { Id = property.Id, Language = property.Language, Value = string.Join(Constants.Occtoo.MultiValueDefaultSeparator, existingValues) });
                    }
                }
            }
        }
    }
}