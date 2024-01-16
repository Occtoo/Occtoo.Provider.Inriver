using Occtoo.Generic.Inriver.Model.Enums;
using System.Collections.Generic;

namespace Occtoo.Generic.Inriver.Model.Settings
{
    public class MergeSettings
    {
        public MergeSettings()
        {
            Type = MergeType.None;
        }

        public MergeType Type { get; set; }
        public LinkUpdateType LinkUpdateType { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public string DataSource { get; set; }
        public string PropertyAlias { get; set; }

        /// <summary>
        /// list of fields to be imported - null or empty means all fields
        /// key: field id, value: localize
        /// </summary>
        public Dictionary<string, bool> Fields { get; set; }
    }
}