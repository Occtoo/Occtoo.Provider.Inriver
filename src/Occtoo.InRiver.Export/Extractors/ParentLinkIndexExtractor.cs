using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class ParentLinkIndexExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public ParentLinkIndexExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var links = _context.ExtensionManager.DataService.GetInboundLinksForEntityAndLinkType(inRiverEntity.Id,
                settings.Id);
            if (!links.Any()) return;

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = links.First().Index.ToString()
            });
        }
    }
}