using inRiver.Remoting.Extension;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Model.Enums;
using System.Collections.Generic;

namespace Occtoo.Generic.Inriver.Extractors.Factory
{
    public interface IExtractorsFactory
    {
        IExceptionFieldExtractor CreateExtractor(ExceptionFieldType type, List<string> uniqueIdFields);
    }

    public class ExtractorsFactory : IExtractorsFactory
    {
        private readonly Settings _settings;
        private readonly inRiverContext _context;

        public ExtractorsFactory(Settings settings,
            inRiverContext context)
        {
            _settings = settings;
            _context = context;
        }

        public IExceptionFieldExtractor CreateExtractor(ExceptionFieldType type,
            List<string> uniqueIdFields)
        {
            switch (type)
            {
                case ExceptionFieldType.String:
                    return new DefaultExtractor();

                case ExceptionFieldType.CvlKeys:
                    return new CvlKeysExtractor();

                case ExceptionFieldType.ClearChars:
                    return new ClearCharsExtractor(_settings, _context);

                case ExceptionFieldType.XmlSku:
                    return new XmlSkuExtractor(_context);

                case ExceptionFieldType.FieldSet:
                    return new FieldSetExtractor();

                case ExceptionFieldType.FieldEmpty:
                    return new FieldEmptyExtractor();

                case ExceptionFieldType.EmptyList:
                    return new EmptyListExtractor(_context);

                case ExceptionFieldType.ListOfChannels:
                    return new ListOfChannelsExtractor(_context);

                case ExceptionFieldType.HasChildrenByFieldValue:
                    return new HasChildrenByFieldValueExtractor(_context);

                case ExceptionFieldType.PositionInTree:
                    return new PositionInChannelTreeExtractor(_context);

                case ExceptionFieldType.Custom:
                    return new CustomExtractor();

                case ExceptionFieldType.ParentFieldValues:
                case ExceptionFieldType.ChildrenFieldValues:
                    return new LinkedEntityFieldValuesExtractor(_context);

                case ExceptionFieldType.FieldContainsValues:
                    return new FieldContainsValuesExtractor();

                case ExceptionFieldType.FieldValuesEqual:
                    return new FieldValuesEqualExtractor();

                case ExceptionFieldType.ChildMediaId:
                    return new ChildMediaIdExtractor(_settings, _context);

                case ExceptionFieldType.ChildMediaUrl:
                    return new ChildMediaUrlExtractor(_settings, _context);

                case ExceptionFieldType.LocalValue:
                    return new LocalValueExtractor(_context);

                case ExceptionFieldType.ParentFieldLocalizedValues:
                case ExceptionFieldType.ChildrenFieldLocalizedValues:
                    return new LinkedEntityLocalizedFieldValuesExtractor(_settings, _context);

                case ExceptionFieldType.Substring:
                    return new SubstringExtractor();

                case ExceptionFieldType.BelongsToChannelById:
                    return new BelongsToChannelByIdExtractor(_context);

                case ExceptionFieldType.BelongsToChannelByCriteria:
                    return new BelongsToChannelsByCriteriaExtractor(_context);

                case ExceptionFieldType.StructuredEntityField:
                    return new StructureEntityFieldExtractor(_context);

                case ExceptionFieldType.ParentLinkIndex:
                    return new ParentLinkIndexExtractor(_context);

                case ExceptionFieldType.CvlKeysMapping:
                    return new CvlKeysMappingExtractor();

                default:
                    return null;
            }
        }
    }
}