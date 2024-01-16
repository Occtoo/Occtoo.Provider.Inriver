using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class ListOfChannelsExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public ListOfChannelsExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var channelIds = _context.ExtensionManager.ChannelService.GetChannelsForEntity(inRiverEntity.Id);
            if (channelIds.Any())
            {
                dynamicEntity.Properties.Add(new DynamicProperty
                {
                    Id = settings.Alias,
                    Language = string.Empty,
                    Value = string.Join(Constants.Occtoo.MultiValueDefaultSeparator, channelIds)
                });
            }
        }
    }
}