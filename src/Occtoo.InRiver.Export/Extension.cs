using inRiver.Remoting.Extension;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using Occtoo.Generic.Inriver.Model.ConnectorStates;
using System;
using System.Collections.Generic;

namespace Occtoo.Generic.Inriver
{
    public class Extension : IEntityListener, ILinkListener
    {
        #region init
        private readonly JsonSerializerSettings _serializingSettings;

        public Extension()
        {
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

        public Dictionary<string, string> DefaultSettings => new Dictionary<string, string>();

        public string Test()
        {
            Context.Log(LogLevel.Information, "Test function run");
            return $"Extension {Context.ExtensionId} loaded correctly";
        }

        #endregion init

        #region entity listener

        public void EntityCreated(int entityId)
        {

            try
            {
                var entity = new Entity() { Id = entityId };
                var data = JsonConvert.SerializeObject(new EntityListenerStateData
                {
                    Entity = entity,
                    Event = Constants.ConnectorState.EntityCreated
                });
                Context.ExtensionManager.UtilityService.AddConnectorState(new ConnectorState
                {
                    ConnectorId = Constants.ConnectorState.ConnectorId,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, "Occtoo Export - error while creating connector state for created event.", ex);
                throw;
            }
        }

        public void EntityUpdated(int entityId, string[] fields)
        {
            try
            {
                var entity = new Entity() { Id = entityId };
                var data = JsonConvert.SerializeObject(new EntityListenerStateData
                {
                    Entity = entity,
                    Event = Constants.ConnectorState.EntityUpdated,
                    Fields = fields
                });
                Context.ExtensionManager.UtilityService.AddConnectorState(new ConnectorState
                {
                    ConnectorId = Constants.ConnectorState.ConnectorId,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, "Occtoo Export - error while creating connector state for updated event.", ex);
                throw;
            }
        }

        public void EntityDeleted(Entity entity)
        {
            try
            {
                var data = JsonConvert.SerializeObject(new EntityListenerStateData
                {
                    Entity = entity,
                    Event = Constants.ConnectorState.EntityDeleted
                }, _serializingSettings);
                Context.ExtensionManager.UtilityService.AddConnectorState(new ConnectorState
                {
                    ConnectorId = Constants.ConnectorState.ConnectorId,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, "Occtoo Export - error while creating connector state for deleted event.", ex);
                throw;
            }
        }

        #endregion entity listener

        #region link listener

        public void LinkCreated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            try
            {
                var data = JsonConvert.SerializeObject(new LinkListenerStateData
                {
                    LinkId = linkId,
                    SourceId = sourceId,
                    TargetId = targetId,
                    LinkTypeId = linkTypeId,
                    LinkEntityId = linkEntityId,
                    Event = Constants.ConnectorState.LinkCreated
                });
                Context.ExtensionManager.UtilityService.AddConnectorState(new ConnectorState
                {
                    ConnectorId = Constants.ConnectorState.ConnectorId,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, "Occtoo Export - error while processing link created event.", ex);
                throw;
            }
        }


        public void LinkDeleted(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            try
            {
                var data = JsonConvert.SerializeObject(new LinkListenerStateData
                {
                    LinkId = linkId,
                    SourceId = sourceId,
                    TargetId = targetId,
                    LinkTypeId = linkTypeId,
                    LinkEntityId = linkEntityId,
                    Event = Constants.ConnectorState.LinkDeleted
                });
                Context.ExtensionManager.UtilityService.AddConnectorState(new ConnectorState
                {
                    ConnectorId = Constants.ConnectorState.ConnectorId,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, "Occtoo Export - error while processing link deleted event.", ex);
                throw;
            }
        }

        public void LinkUpdated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            LinkCreated(linkId, sourceId, targetId, linkTypeId, linkEntityId);
        }

        public void LinkActivated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            LinkCreated(linkId, sourceId, targetId, linkTypeId, linkEntityId);
        }

        public void LinkInactivated(int linkId, int sourceId, int targetId, string linkTypeId, int? linkEntityId)
        {
            LinkDeleted(linkId, sourceId, targetId, linkTypeId, linkEntityId);
        }

        #endregion link listener

        #region not used events

        public void EntityLocked(int entityId)
        {
        }

        public void EntityUnlocked(int entityId)
        {
        }

        public void EntityFieldSetUpdated(int entityId, string fieldSetId)
        {
        }

        public void EntityCommentAdded(int entityId, int commentId)
        {
        }

        public void EntitySpecificationFieldAdded(int entityId, string fieldName)
        {
        }

        public void EntitySpecificationFieldUpdated(int entityId, string fieldName)
        {
        }

        #endregion not used events
    }
}