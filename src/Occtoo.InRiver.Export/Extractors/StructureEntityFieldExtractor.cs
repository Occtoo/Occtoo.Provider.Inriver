using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class StructureEntityFieldExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public StructureEntityFieldExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Params)) return;

            var (channelId, entityType) = ParseParams(settings.Params);

            if (string.IsNullOrEmpty(entityType) || !channelId.HasValue) return;

            var structuredEntities =
                _context.ExtensionManager.ChannelService.GetAllChannelStructureEntitiesForType(channelId.Value, entityType).Where(e => e.EntityId == inRiverEntity.Id).ToList();

            var values = (
                from structuredEntity in structuredEntities
                let propertyInfo = structuredEntity.GetType().GetProperty(settings.Id)
                where propertyInfo != null
                select propertyInfo.GetValue(structuredEntity)?.ToString()
                into value
                where !string.IsNullOrEmpty(value)
                select value
            ).ToList();

            if (values.Any())
            {
                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = string.Empty,
                    Value = string.Join(Constants.Occtoo.MultiValueDefaultSeparator, values)
                });
            }
        }

        private (int? channelId, string entityType) ParseParams(string settingsParams)
        {
            var paramParts = settingsParams.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            return paramParts.Length == 2 &&
                   int.TryParse(paramParts[0], out var channelId)
                ? (channelId, paramParts[1])
                : ((int? channelId, string entityType))(null, null);
        }
    }
}