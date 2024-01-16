using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Helpers;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class CvlKeysExtractor : IExceptionFieldExtractor
    {
        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            dynamicEntity.Properties.Add(ValueHelpers.GetValue(settings.Alias,
                ValueHelpers.GetCvlKeys(inRiverEntity.GetField(settings.Id)?.Data), string.Empty));
        }
    }
}