using System;
using System.Collections.Generic;

namespace Occtoo.Generic.Inriver.Model.Settings
{
    public class LinkEntityJsonSettings
    {
        public string Direction { get; set; } = "Outbound";

        public string TargetIdentifierField { get; set; }

        //Leaving this list null or empty means all link entity fields will be included.
        public List<string> Fields { get; set; }

        //Possibility to override the fieldTypeIds and replace them with other aliases.
        public Dictionary<string, string> FieldAliases { get; set; }

        public bool TryGetDirection(out bool isInbound)
        {
            isInbound = false;

            if (string.IsNullOrWhiteSpace(Direction))
                return true;

            var direction = Direction.Trim();

            if (string.Equals(direction, "Outbound", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.Equals(direction, "Inbound", StringComparison.OrdinalIgnoreCase))
            {
                isInbound = true;
                return true;
            }

            return false;
        }
    }
}