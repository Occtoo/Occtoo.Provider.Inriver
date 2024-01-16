using inRiver.Remoting.Extension;
using inRiver.Remoting.Objects;
using inRiver.Remoting.Query;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class BelongsToChannelsByCriteriaExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public BelongsToChannelsByCriteriaExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Params)) return;

            var settingsParams = settings.Params.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            if (settingsParams.Length != 2) return;

            var complexQuery = new ComplexQuery
            {
                EntityTypeId = "Channel"
            };

            var criteria = new Criteria()
            {
                FieldTypeId = settingsParams[0],
                Operator = Operator.Equal,
                Value = ExtractValue(settingsParams[1])
            };

            complexQuery.DataQuery = new Query()
            {
                Criteria = new List<Criteria>() { criteria },
                Join = Join.And
            };

            var isInChannel = false;
            var channelIds = _context.ExtensionManager.DataService.Search(complexQuery, LoadLevel.DataOnly).Select(x => x.Id);
            foreach (var channelId in channelIds)
            {
                isInChannel = _context.ExtensionManager.ChannelService.EntityExistsInChannel(channelId, inRiverEntity.Id);
                if (isInChannel) break;
            }

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = isInChannel.ToString()
            });
        }

        private object ExtractValue(string settingsParam)
        {
            if (bool.TryParse(settingsParam, out var b))
            {
                return b;
            }

            return settingsParam;
        }
    }
}
