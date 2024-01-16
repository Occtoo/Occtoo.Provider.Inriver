using Occtoo.Generic.Inriver.Model.Enums;
using System.Collections.Generic;

namespace Occtoo.Generic.Inriver.Model.Settings
{
    public class EntitySettings
    {
        public EntitySettings()
        {
            ChildrenMerges = new List<MergeSettings>();
            ParentsMerges = new List<MergeSettings>();
            UniqueIdFields = new List<string>();
            Filters = new List<FilterSettings>();
            FieldFilterSettings = new List<FieldFilterSettings>();
            IgnoreList = new List<string>();
            Fields = new Dictionary<string, FieldSettings>();
            Type = EntityType.Entity;
            ExceptionFields = new List<ExceptionFieldSettings>();
            SkipInFullExport = false;
        }

        /// <summary>
        /// entity name used in inRiver
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// data source id where entity will be stored in onboarding
        /// </summary>
        public string DataSource { get; set; }

        /// <summary>
        /// list of field IDs that will be used to create key in occtoo
        /// field values will be concatenated with "_" as separator
        /// NOTE: for media/resource - first value in the list must be the resource file name field (e.g. ResourceFilename) and second value in the list must be the resource file id field (e.g. ResourceFileId), other fields can be added in any order but fields must be available on resource level to be able to delete resource and fields must be mandatory otherwise resources will not be created at all
        /// </summary>
        public List<string> UniqueIdFields { get; set; }

        /// <summary>
        /// list of field IDs that will be skipped during export
        /// </summary>
        public List<string> IgnoreList { get; set; }

        /// <summary>
        /// property name where inRiver system id is going to be stored
        /// </summary>
        public string EntityIdAlias { get; set; }

        /// <summary>
        /// defines entity type and controls which action is going to be taken
        /// Entity, Media and Sku
        /// </summary>
        public EntityType Type { get; set; }

        /// <summary>
        /// list of filters that will define rules if entity will be
        /// filtered out (skipped) from export 
        /// </summary>
        public List<FilterSettings> Filters { get; set; }

        /// <summary>
        /// list of fields and following rules used to avoid updating
        /// entity on some field changed
        /// </summary>
        public List<FieldFilterSettings> FieldFilterSettings { get; set; }

        /// <summary>
        /// list of child entities that will be merged together with main
        /// entity in same data source
        /// </summary>
        public List<MergeSettings> ChildrenMerges { get; set; }

        /// <summary>
        /// list of parent entities that will be merged together with main
        /// entity in same data source
        /// </summary>
        public List<MergeSettings> ParentsMerges { get; set; }

        /// <summary>
        /// list of fields that will be processed by special rules,
        /// every type of exception field has its own function that
        /// extracts data
        /// </summary>
        public List<ExceptionFieldSettings> ExceptionFields { get; set; }

        /// <summary>
        /// list of fields to be imported - null or empty means all fields
        /// key: field id
        /// </summary>
        public Dictionary<string, FieldSettings> Fields { get; set; }

        /// <summary>
        /// flag entity type to be skipped when performing full export
        /// default value is false
        /// </summary>
        public bool SkipInFullExport { get; set; }

        /// <summary>
        /// flag all non-fully merged parents to be updated on entity change
        /// </summary>
        public bool UpdateParentsOnChange { get; set; }
    }
}