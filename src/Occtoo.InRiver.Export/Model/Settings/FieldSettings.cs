using Occtoo.Generic.Inriver.Model.Enums;
using System.Collections.Generic;

namespace Occtoo.Generic.Inriver.Model.Settings
{
    public class FieldSettings
    {
        public FieldSettings()
        {
            SkuType = SkuType.None;
            SkuIds = new List<string>();
        }

        public bool IsLocalized { get; set; }
        public string Alias { get; set; }
        public bool IsSku { get; set; }
        public SkuType SkuType { get; set; }
        public string PathToProps { get; set; }
        public List<string> SkuIds { get; set; }
    }
}