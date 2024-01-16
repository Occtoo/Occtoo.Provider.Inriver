using inRiver.Remoting.Extension;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using Occtoo.Generic.Infrastructure;
using Occtoo.Generic.Infrastructure.Base;
using Occtoo.Generic.Infrastructure.Extensions;
using Occtoo.Generic.Inriver.Model.ConnectorStates;
using Occtoo.Generic.Inriver.Services;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Occtoo.Generic.Inriver
{
    public class SendToOcctooExtension : IScheduledExtension
    {
        #region init

        private readonly ExtensionInitialization<Settings> _initialization;
        private readonly JsonSerializerSettings _serializingSettings;
        public SendToOcctooExtension() : this(new Startup())
        {
        }

        public SendToOcctooExtension(IExtensionStartup startup)
        {
            try
            {
                _initialization = new ExtensionInitialization<Settings>(startup);
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.ToString());
            }

            try
            {
                _serializingSettings = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Serialize };
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.ToString());
            }
        }

        public inRiverContext Context { get; set; }

        public Dictionary<string, string> DefaultSettings { get; } = SettingsHelper.AsDefaultSettings<Settings>();

        public string Test()
        {
            return $"Extension {Context.ExtensionId} loaded correctly";
        }

        #endregion init

        public void Execute(bool force)
        {
            try
            {
                var exporterService = _initialization.GetService<IExporterService>(Context);
                var documentService = _initialization.GetService<IDocumentsService>(Context);
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                // Get connector states
                var connectorStates = Context.ExtensionManager.UtilityService.GetAllConnectorStatesForConnector(Constants.ConnectorState.ConnectorId);
                Context.Log(LogLevel.Information, $"Found {connectorStates.Count} connectorstates. {stopwatch.Elapsed}");

                // Sort connector states
                var entityListenerEvents = new List<EntityListenerStateData>();
                var linkListenerEvents = new List<LinkListenerStateData>();
                SortConnectorStates(connectorStates, entityListenerEvents, linkListenerEvents);

                // EntityListener Events
                var errorEntityListenerStates = new List<EntityListenerStateData>();
                var documents = HandleEntityListenerEvents(exporterService, stopwatch, entityListenerEvents, errorEntityListenerStates);
                Context.Log(LogLevel.Information, $"All entityEvents handled. {stopwatch.Elapsed}");
                SendToOcctoo(documents, documentService, entityListenerEvents, errorEntityListenerStates);
                Context.Log(LogLevel.Information, $"Sent all entityEvents to Occtoo. {stopwatch.Elapsed}");

                // LinkListener Events
                var errorLinkListenerStates = new List<LinkListenerStateData>();
                documents = HandleLinkListenerEvents(exporterService, linkListenerEvents, errorLinkListenerStates);
                Context.Log(LogLevel.Information, $"All linkEvents handled. {stopwatch.Elapsed}");
                SendToOcctoo(documents, documentService, linkListenerEvents, errorLinkListenerStates);
                Context.Log(LogLevel.Information, $"Sent all linkEvents to Occtoo. {stopwatch.Elapsed}");

                // Removing completed connector states
                HandleConnectorStates(connectorStates, errorEntityListenerStates, errorLinkListenerStates);
                Context.Log(LogLevel.Information, $"Deleted all sent events. {stopwatch.Elapsed}");
                stopwatch.Stop();
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, "Occtoo Export - error while performing connectorstate export.", ex);
                throw;
            }
        }

        private static void SortConnectorStates(List<ConnectorState> connectorStates, List<EntityListenerStateData> entityListenerEvents, List<LinkListenerStateData> linkListenerEvents)
        {
            foreach (var state in connectorStates)
            {
                var indata = JsonConvert.DeserializeObject<BaseStateData>(state.Data);
                switch (indata.Event)
                {
                    case Constants.ConnectorState.EntityCreated:
                    case Constants.ConnectorState.EntityUpdated:
                    case Constants.ConnectorState.EntityDeleted:
                        var entityListenerData = JsonConvert.DeserializeObject<EntityListenerStateData>(state.Data);
                        entityListenerData.Created = state.Created;
                        entityListenerEvents.Add(entityListenerData);
                        break;
                    case Constants.ConnectorState.LinkCreated:
                    case Constants.ConnectorState.LinkUpdated:
                    case Constants.ConnectorState.LinkDeleted:
                        var linkListenerData = JsonConvert.DeserializeObject<LinkListenerStateData>(state.Data);
                        linkListenerData.Created = state.Created;
                        linkListenerEvents.Add(linkListenerData);
                        break;
                    default:
                        break;
                }
            }
        }

        private List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> HandleEntityListenerEvents(IExporterService exporterService, Stopwatch stopwatch, List<EntityListenerStateData> entityListenerEvents, List<EntityListenerStateData> errorEntityListenerStates)
        {
            //Group to only do one update per Entity Id
            var entityEventsGrouped = entityListenerEvents.OrderByDescending(y => y.Created).GroupBy(x => x.Entity.Id);
            Context.Log(LogLevel.Information, $"Grouped {entityEventsGrouped.Count()} entityEvents. {stopwatch.Elapsed}");
            var documents = new List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)>();
            foreach (var group in entityEventsGrouped)
            {
                var entityListenerData = group.First();
                try
                {

                    var entity = entityListenerData.Entity;
                    if (entityListenerData.Event != Constants.ConnectorState.EntityDeleted)
                    {
                        entity = Context.ExtensionManager.DataService.GetEntity(entityListenerData.Entity.Id, LoadLevel.DataOnly);
                    }
                    switch (entityListenerData.Event)
                    {
                        case Constants.ConnectorState.EntityCreated:
                            documents.AddRange(exporterService.EntityChanged(entity, entity.Id, new List<string>()));
                            break;
                        case Constants.ConnectorState.EntityUpdated:
                            documents.AddRange(exporterService.EntityChanged(entity, entity.Id, entityListenerData.Fields));
                            break;
                        case Constants.ConnectorState.EntityDeleted:
                            documents.AddRange(exporterService.EntityChanged(entity, entity.Id, new List<string>(), true));
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Context.Log(LogLevel.Error, "EntityListenerEvent handling error (event requeued)", ex);
                    errorEntityListenerStates.Add(entityListenerData);
                }
            }

            return documents;
        }

        private List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> HandleLinkListenerEvents(IExporterService exporterService, List<LinkListenerStateData> linkListenerEvents, List<LinkListenerStateData> errorLinkListenerStates)
        {
            //Group to only do one update per Link Id
            var linkEventsGrouped = linkListenerEvents.OrderByDescending(y => y.Created).GroupBy(x => x.LinkId);
            Context.Log(LogLevel.Information, $"Grouped {linkEventsGrouped.Count()} linkEvents.");
            var documents = new List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)>();
            foreach (var group in linkEventsGrouped)
            {
                var linkListenerData = group.First();
                try
                {
                    documents.AddRange(exporterService.LinkChanged(linkListenerData.LinkId, linkListenerData.SourceId, linkListenerData.TargetId, linkListenerData.LinkTypeId, linkListenerData.LinkEntityId));
                }
                catch (Exception ex)
                {
                    Context.Log(LogLevel.Error, "LinkListenerEvent handling error (event requeued)", ex);
                    errorLinkListenerStates.Add(linkListenerData);
                }
            }

            return documents;
        }

        private static void SendToOcctoo<T>(List<(DynamicEntity Document, string Type, string EntitySystemIdAlias)> documents, IDocumentsService documentService, List<T> events, List<T> errorEvents) where T : BaseStateData
        {
            if (documents == null || !documents.Any())
            {
                return;
            }

            var errorKeys = new List<int>();
            var groupedDocs = documents.GroupBy(x => x.Type);
            foreach (var group in groupedDocs)
            {
                // Send them in batches of 500
                foreach (var batch in group.Select(x => x.Document).Batch(500))
                {
                    errorKeys.AddRange(documentService.SendDocuments(group.Key, batch.ToList(), group.First().EntitySystemIdAlias));
                }
            }

            if (errorKeys.Any())
            {
                errorEvents.AddRange(events.Where(x => errorKeys.Contains(x.Id)));
            }
        }

        private void HandleConnectorStates(List<ConnectorState> connectorStates, List<EntityListenerStateData> errorEntityListenerStates, List<LinkListenerStateData> errorLinkListenerStates)
        {
            connectorStates.ForEach(x => Context.ExtensionManager.UtilityService.DeleteConnectorState(x.Id));
            foreach (var state in errorEntityListenerStates)
            {
                Context.ExtensionManager.UtilityService.AddConnectorState(new ConnectorState
                {
                    ConnectorId = Constants.ConnectorState.ConnectorId,
                    Data = JsonConvert.SerializeObject(state, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Serialize })
                });
            }

            foreach (var state in errorLinkListenerStates)
            {
                Context.ExtensionManager.UtilityService.AddConnectorState(new ConnectorState
                {
                    ConnectorId = Constants.ConnectorState.ConnectorId,
                    Data = JsonConvert.SerializeObject(state, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Serialize })
                });
            }
        }
    }
}
