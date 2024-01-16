using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;

namespace Occtoo.Generic.Inriver.Extractors.Interfaces
{
    public interface IExceptionFieldExtractor
    {
        void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings);
    }
}