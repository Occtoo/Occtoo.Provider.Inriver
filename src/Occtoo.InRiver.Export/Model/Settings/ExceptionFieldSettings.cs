using Occtoo.Generic.Inriver.Model.Enums;

namespace Occtoo.Generic.Inriver.Model.Settings
{
    public class ExceptionFieldSettings
    {
        public ExceptionFieldSettings()
        {
            Type = ExceptionFieldType.String;
        }

        public string Id { get; set; }
        public string Alias { get; set; }
        public ExceptionFieldType Type { get; set; }
        public string Params { get; set; }
    }
}
