using Occtoo.Generic.Infrastructure;
using Occtoo.Generic.Inriver.Model.Settings;

namespace Occtoo.Generic.Inriver
{
    public class Settings
    {
        public string Environment { get; set; }
        public string DefaultLanguage { get; set; }
        public string DocumentIdForbiddenChars { get; set; }
        public bool SkipFilteredEntities { get; set; } = false;
        public bool SkipMediaKeyGuidTransform { get; set; } = false;
        public string OcctooDataProviderId { get; set; }
        public string OcctooDataProviderSecret { get; set; }
        [Json]
        public ExportSettings ExportSettings { get; set; }
    }
}