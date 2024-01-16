using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors.Model
{
    public class ChildMediaSettings
    {
        /// <summary>
        /// defines if children should be sorted per link index
        /// default value is true
        /// </summary>
        [JsonProperty("order")]
        public bool Order { get; set; } = true;

        /// <summary>
        /// defines if first child should be taken, otherwise last one
        /// default value is true
        /// </summary>
        [JsonProperty("first")]
        public bool First { get; set; } = true;

        /// <summary>
        /// field if that must be matched by value
        /// </summary>
        [JsonProperty("primaryFieldId")]
        public string PrimaryFieldId { get; set; }

        /// <summary>
        /// field values that must be matched (first value - highest priority)
        /// </summary>
        [JsonProperty("primaryFieldValues")]
        public List<string> PrimaryFieldValues { get; set; }

        /// <summary>
        /// secondary field that might be matched if any
        /// </summary>
        [JsonProperty("secondaryFieldId")]
        public string SecondaryFieldId { get; set; }

        /// <summary>
        /// secondary field values that must be matched (first value - highest priority)
        /// </summary>
        [JsonProperty("secondaryFieldValues")]
        public List<string> SecondaryFieldValues { get; set; }

        /// <summary>
        /// list of fields that are used to crate media unique id
        /// </summary>
        [JsonProperty("uniqueFieldIds")]
        public List<string> UniqueFieldIds { get; set; }

        public bool Valid()
        {
            return UniqueFieldIds != null &&
                        UniqueFieldIds.Any() &&
                        !string.IsNullOrEmpty(PrimaryFieldId) &&
                        PrimaryFieldValues != null &&
                        PrimaryFieldValues.Any() &&
                        (string.IsNullOrEmpty(SecondaryFieldId) ||
                         SecondaryFieldValues != null && SecondaryFieldValues.Any());
        }
    }
}