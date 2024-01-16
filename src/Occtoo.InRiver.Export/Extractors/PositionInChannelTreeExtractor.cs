using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class PositionInChannelTreeExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public PositionInChannelTreeExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var position = string.Empty;
            var parentLink = _context
                                 .ExtensionManager
                                 .DataService
                                 .GetInboundLinksForEntityAndLinkType(inRiverEntity.Id, settings.Id)
                                 .FirstOrDefault()
                             ?? _context
                                 .ExtensionManager
                                 .DataService
                                 .GetInboundLinksForEntityAndLinkType(inRiverEntity.Id, settings.Params)
                                 .FirstOrDefault();

            if (parentLink != null)
            {
                position = parentLink.Index.ToString();
            }

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = position
            });
        }
    }
}