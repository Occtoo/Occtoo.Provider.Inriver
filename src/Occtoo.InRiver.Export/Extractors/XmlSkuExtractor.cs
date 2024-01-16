using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Helpers;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class XmlSkuExtractor : IExceptionFieldExtractor
    {
        private readonly inRiverContext _context;

        public XmlSkuExtractor(inRiverContext context)
        {
            _context = context;
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var skuField = inRiverEntity.GetField(settings.Id);

            if (skuField == null) return;

            if (skuField.FieldType.DataType != DataType.Xml &&
                skuField.FieldType.DataType != DataType.String &&
                !string.IsNullOrEmpty(settings.Params)) return;

            var properties = GetSkuProperties(settings, skuField.Data?.ToString());
            if (properties.Any())
            {
                dynamicEntity.Properties.AddRange(properties);
            }
        }

        private List<DynamicProperty> GetSkuProperties(ExceptionFieldSettings settings, string xml)
        {
            if (string.IsNullOrEmpty(xml)) return new List<DynamicProperty>();

            try
            {
                var xDoc = XDocument.Parse(xml);
                var paramsData = settings.Params.Split(new[] { Constants.Occtoo.MultiValueDefaultSeparator }, StringSplitOptions.RemoveEmptyEntries);
                var attributes = paramsData.Where(x => x.StartsWith("@")).Select(x => x.Substring(1)).ToList();
                var elements = paramsData.Where(x => !x.StartsWith("@")).ToList();
                var response = new List<DynamicProperty>();

                XmlHelpers.ExtractXmlData(xDoc.Root?.Elements().ToList(), response, attributes, elements, settings.Alias);

                return response;
            }
            catch (Exception ex)
            {
                _context.Logger.Log(LogLevel.Warning, "Size and EANs are not formatted well.", ex);
                return new List<DynamicProperty>();
            }
        }
    }
}