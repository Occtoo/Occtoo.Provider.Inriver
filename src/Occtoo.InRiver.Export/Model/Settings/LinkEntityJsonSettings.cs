using System.Collections.Generic;

namespace Occtoo.InRiver.Export.Model.Settings
{
    public class LinkEntityJsonSettings
    {
        public string Direction { get; set; } = "Outbound";

        public string TargetIdentifierField { get; set; }

        //Leaving this list null or empty means all link entity fields will be included.
        public List<string> Fields { get; set; }

        //Possibility to override the fieldTypeIds and replace them with other aliases.
        public Dictionary<string, string> FieldAliases { get; set; }
    }
}