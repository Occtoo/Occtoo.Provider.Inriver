namespace Occtoo.Generic.Inriver.Model.Enums
{
    public enum ExceptionFieldType
    {
        String = 0,
        CvlKeys = 1,
        ClearChars = 2,
        XmlSku = 3,
        FieldSet = 4,
        /// <summary>
        /// checks if field is empty and creates exceptional field by alias (boolean)
        /// properties:
        /// Id => field id,
        /// Alias => on boarding property name,
        /// Params => "null/inverse" ->
        /// if params is set to inverse boolean value will be set to inverse value
        /// </summary>
        FieldEmpty = 5,
        ListOfChannels = 6,
        /// <summary>
        /// creates property with predefined value in case that cvl field is empty
        /// properties:
        /// Id => field id,
        /// Alias => on boarding property name,
        /// Params => "predefined value"
        /// </summary>
        EmptyList = 7,
        /// <summary>
        /// creates additional field (boolean) if entity contains any child
        /// that satisfies conditions
        /// properties:
        /// Id => link id,
        /// Alias => on boarding property name,
        /// Params => "InRiverFieldId|equal/any/all/anything|value1|value2|..."
        /// </summary>
        HasChildrenByFieldValue = 8,
        /// <summary>
        /// Adds a field with the position the node has in the channel tree structure 0-N
        /// Id => link id,
        /// Alias => on boarding property name,
        /// Params => alternative link type
        /// </summary>
        PositionInTree = 9,
        /// <summary>
        /// Custom exporter type -> used for very specific calculations that cannot be generic
        /// </summary>
        Custom = 10,
        /// <summary>
        /// Adds a list of children field values as new property
        /// Id => link id,
        /// Alias => on boarding property name
        /// Params => "parentFieldId1;parentFieldId2...|sort|startIndex1;startIndex2" ->
        /// if multiple fields selected values will be concatenated with ":"
        /// sort (true/false)
        /// if start index set take substring (multiple items, require multiple indexes)
        /// (if index has negative value it will take number of chars from end)
        /// </summary>
        ParentFieldValues = 11,
        /// <summary>
        /// Adds a list of children field values as new property
        /// Id => link id,
        /// Alias => on boarding property name
        /// Params => "childFieldId1;childFieldId2...|sort|startIndex1;startIndex2" ->
        /// if multiple fields selected values will be concatenated with ":"
        /// sort (true/false)
        /// if start index set take substring (multiple items, require multiple indexes)
        /// (if index has negative value it will take number of chars from end)
        /// </summary>
        ChildrenFieldValues = 12,
        /// <summary>
        /// Adds boolean flag based on field value, works with cvl fields and
        /// string fields (in case of string supports only one value in params)
        /// ID => field id,
        /// Alias => on boarding property name
        /// Params => "all/any|value1;value2;value3..."
        /// </summary>
        FieldContainsValues = 13,
        /// <summary>
        /// Adds boolean flag based on field value, true if values are the same
        /// ID => field id,
        /// Alias => on boarding property name
        /// Params => comparing field id|extractIfOneOfFieldsIsEmpty
        /// </summary>
        FieldValuesEqual = 14,
        /// <summary>
        /// Adds property based on media children data (occtoo media id)
        /// ID => link id,
        /// Alias => on boarding property name
        /// Params => it is json value serialized from
        /// Occtoo.InRiver.Export.Extractors.Model.ChildMediaSettings
        /// </summary>
        ChildMediaId = 15,
        /// <summary>
        /// Adds property based on media children data (occtoo media url)
        /// ID => link id,
        /// Alias => on boarding property name
        /// Params => it is json value serialized from
        /// Occtoo.InRiver.Export.Extractors.Model.ChildMediaSettings
        /// </summary>
        ChildMediaUrl = 16,
        /// <summary>
        /// Adds localized value of field as new property
        /// ID => field id,
        /// Alias => on boarding property name
        /// Params => language
        /// </summary>
        LocalValue = 17,
        /// <summary>
        /// Adds a list of parents field values as new property
        /// Id => link id,
        /// Alias => on boarding property name
        /// Params => "inRiver parent field id|defaultSubstitutionField|mappings"
        /// if params contains only inRiver parent field that field will be extracted
        /// if params contains default substitution field
        /// system will use mapping values to select substitution field to be extracted
        /// </summary>
        ParentFieldLocalizedValues = 18,
        /// <summary>
        /// Adds a list of parents field values as new property
        /// Id => link id,
        /// Alias => on boarding property name
        /// Params => "inRiver child field id|defaultSubstitutionField|mappings"
        /// if params contains only inRiver parent field that field will be extracted
        /// if params contains default substitution field
        /// system will use mapping values to select substitution field to be extracted
        /// </summary>
        ChildrenFieldLocalizedValues = 19,
        /// <summary>
        /// Adds substring value of field as new property
        /// ID => field id,
        /// Alias => on boarding property name
        /// Params => offset (if offset is negative it will be taken from end of value)
        /// </summary>
        Substring = 20,
        /// <summary>
        /// Adds property with boolean value if entity belongs to channel
        /// ID => field id,
        /// Alias => on boarding property name
        /// Params => channel id
        /// </summary>
        BelongsToChannelById = 21,
        /// <summary>
        /// Adds property with boolean value if entity belongs to channel
        /// ID => field id,
        /// Alias => on boarding property name
        /// Params => "fieldId|value"
        /// </summary>
        BelongsToChannelByCriteria = 22,
        /// <summary>
        /// Adds property with related structured entities values
        /// ID => structured entity field name,
        /// Alias => on boarding property name
        /// Params => "ChannelId|EntityTypeId"
        /// </summary>
        StructuredEntityField = 23,
        /// <summary>
        /// Adds property with link index to parent entity
        /// ID => link id,
        /// Alias => on boarding property name
        /// Params => ""
        /// </summary>
        ParentLinkIndex = 24,
        /// <summary>
        /// Adds property with mapped cvl keys - mapping is controlled by params
        /// ID => field id,
        /// Alias => on boarding property name
        /// Params => "onboardingValue1;cvlKey11:cvlKey12:cvlKey13...|onboardingValue2;cvlKey21:cvlKey22.."
        /// </summary>
        CvlKeysMapping = 25
    }
}