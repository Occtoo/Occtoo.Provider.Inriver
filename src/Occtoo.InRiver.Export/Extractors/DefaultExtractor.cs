using inRiver.Remoting.Objects;
using Occtoo.Generic.Infrastructure.Extensions;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Helpers;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class DefaultExtractor : IExceptionFieldExtractor
    {
        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            dynamicEntity.Properties.Add(ValueHelpers.GetValue(settings.Alias,
                inRiverEntity.GetData<string>(settings.Id), string.Empty));
        }
    }
}