using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Occtoo.Generic.Inriver.Helpers
{
    public static class XmlHelpers
    {
        public static void ExtractXmlData(IReadOnlyCollection<XElement> xElements, List<DynamicProperty> response, ICollection<string> attributes, ICollection<string> elements, string alias)
        {
            if (xElements == null) return;

            foreach (var xElement in xElements)
            {
                foreach (var xAttribute in xElement.Attributes())
                {
                    if (attributes.Contains(xAttribute.Name.LocalName))
                    {
                        ExtractXmlAttributeValue(response, alias, xAttribute);
                    }
                }

                if (xElement.Elements().Any())
                {
                    ExtractXmlData(xElement.Elements().ToList(), response, attributes, elements, alias);
                }
                else
                {
                    if (elements.Contains(xElement.Name.LocalName))
                    {
                        ExtractXmlElementValue(response, alias, xElement);
                    }
                }
            }
        }

        private static void ExtractXmlAttributeValue(ICollection<DynamicProperty> response, string alias, XAttribute xAttribute)
        {
            var prop = response.FirstOrDefault(x => x.Id == $"{alias}{xAttribute.Name.LocalName}");
            if (prop == null)
            {
                prop = new DynamicProperty
                {
                    Id = $"{alias}{xAttribute.Name.LocalName}",
                    Language = string.Empty,
                    Value = xAttribute.Value
                };
                response.Add(prop);
            }
            else
            {
                prop.Value = CreateNewSkuValue(prop.Value, xAttribute.Value);
            }
        }

        private static void ExtractXmlElementValue(ICollection<DynamicProperty> response, string alias, XElement xElement)
        {
            var prop = response.FirstOrDefault(x => x.Id == $"{alias}{xElement.Name.LocalName}");
            if (prop == null)
            {
                prop = new DynamicProperty
                {
                    Id = $"{alias}{xElement.Name.LocalName}",
                    Language = string.Empty,
                    Value = xElement.Value
                };
                response.Add(prop);
            }
            else
            {
                prop.Value = CreateNewSkuValue(prop.Value, xElement.Value);
            }
        }

        private static string CreateNewSkuValue(string propValue, string xmlValue)
        {
            var values = propValue.Split(new[] { Constants.Occtoo.MultiValueDefaultSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (!values.Contains(xmlValue))
            {
                values.Add(xmlValue);
            }

            return string.Join(Constants.Occtoo.MultiValueDefaultSeparator, values);
        }
    }
}