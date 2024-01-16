using Occtoo.Generic.Inriver.Model.Enums;

namespace Occtoo.Generic.Inriver.Model.Settings
{
    public class FieldFilterSettings
    {
        public FieldFilterType Type { get; set; }
        public FilterApplyRule Rule { get; set; }
        public string Value { get; set; }
    }
}