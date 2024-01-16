using inRiver.Remoting;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Occtoo.Generic.Infrastructure.Extensions
{
    public static class InRiverContextExtensions
    {
        public static T GetSettingsAs<T>(this inRiverContext context) where T : new()
        {
            return SettingsHelper.ConvertFromSettingsDictionary<T>(context?.Settings);
        }

        public static Entity GetParentEntity(this IinRiverManager remoteManager, Entity child, string linkTypeId)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (linkTypeId == null) throw new ArgumentNullException(nameof(linkTypeId));
            if (remoteManager == null) throw new ArgumentNullException(nameof(remoteManager));

            return remoteManager.DataService
                .GetInboundLinksForEntityAndLinkType(child.Id, linkTypeId)
                .Select(l => remoteManager.DataService.GetEntity(l.Source.Id, LoadLevel.DataOnly))
                .FirstOrDefault(e => e != null);
        }

        public static List<Entity> GetParentEntities(this IinRiverManager remoteManager, Entity child,
            string linkTypeId)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (linkTypeId == null) throw new ArgumentNullException(nameof(linkTypeId));
            if (remoteManager == null) throw new ArgumentNullException(nameof(remoteManager));

            var parentIds = child.InboundLinks != null && child.InboundLinks.Any()
                ? child.InboundLinks.Where(x => x.LinkType.Id == linkTypeId).Select(x => x.Source.Id).ToList()
                : remoteManager.DataService
                    .GetInboundLinksForEntityAndLinkType(child.Id, linkTypeId)
                    .Select(x => x.Source.Id).ToList();

            return remoteManager.DataService.GetEntities(parentIds, LoadLevel.DataOnly)
                .OrderBy(x => parentIds.IndexOf(x.Id))
                .ToList();
        }

        public static List<Entity> GetChildEntities(this IinRiverManager remoteManager, Entity parent, string linkTypeId)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (linkTypeId == null) throw new ArgumentNullException(nameof(linkTypeId));
            if (remoteManager == null) throw new ArgumentNullException(nameof(remoteManager));

            var childIds = parent.OutboundLinks != null && parent.OutboundLinks.Any()
                ? parent.OutboundLinks.Where(x => x.LinkType.Id == linkTypeId).Select(x => x.Target.Id).ToList()
                : remoteManager.DataService
                    .GetOutboundLinksForEntityAndLinkType(parent.Id, linkTypeId)
                    .OrderBy(l => l.Index)
                    .Select(l => l.Target.Id).ToList();

            return remoteManager.DataService.GetEntities(childIds, LoadLevel.DataOnly)
                .OrderBy(e => childIds.IndexOf(e.Id))
                .ToList();
        }
    }
}