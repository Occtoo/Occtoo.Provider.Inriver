using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class BelongsToChannelByIdExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public BelongsToChannelByIdExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            if (!int.TryParse(settings.Params, out var channelId) || channelId <= 0)
            {
                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = string.Empty,
                    Value = "false"
                });
            }

            var channelIds = _context.ExtensionManager.ChannelService.GetChannelsForEntity(inRiverEntity.Id);

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = channelIds.Contains(channelId).ToString()
            });
        }
    }
}