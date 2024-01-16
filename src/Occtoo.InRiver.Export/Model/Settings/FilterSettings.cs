using Occtoo.Generic.Inriver.Model.Enums;
using System.Collections.Generic;

namespace Occtoo.Generic.Inriver.Model.Settings
{
    public class FilterSettings
    {
        public FilterSettings()
        {
            Language = string.Empty;
        }

        public FilterType Type { get; set; }
        public string Field { get; set; }
        public string Language { get; set; }
        public List<string> Values { get; set; }
    }
}