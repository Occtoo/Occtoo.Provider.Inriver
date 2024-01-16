using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Helpers;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class CustomExtractor : IExceptionFieldExtractor
    {
        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            switch (settings.Alias)
            {
                case "ProductChannelTricepsPDG":
                    ExtractTricepsChannel(dynamicEntity, inRiverEntity, settings);
                    break;
                case "ProductChannelOverwrite":
                    OverwriteProductChannel(dynamicEntity, inRiverEntity, settings);
                    break;
            }
        }

        private void OverwriteProductChannel(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var dynamicProperty = dynamicEntity.Properties.FirstOrDefault(x => x.Id == settings.Id);
            if (dynamicProperty != null)
            {
                dynamicProperty.Value = GetCustomProductChannelValue(inRiverEntity);
                return;
            }

            dynamicEntity.Properties.Add(new DynamicProperty
            { Id = settings.Alias, Language = string.Empty, Value = GetCustomProductChannelValue(inRiverEntity) });
        }

        private string GetCustomProductChannelValue(Entity product)
        {
            string productLineKeys = ValueHelpers.GetCvlKeys(product.GetField("ProductLine").Data);
            string productSegmentKeys = ValueHelpers.GetCvlKeys(product.GetField("ProductSegment").Data);
            string productChannelKeys = ValueHelpers.GetCvlKeys(product.GetField("ProductChannel").Data);

            bool webJewelry = productLineKeys == "JEWELRY" && (productSegmentKeys == "JEWEL" || productSegmentKeys == "NEWJEWELRY") && productChannelKeys.Contains("web");
            bool webWatch = productLineKeys == "WATCH" && (productSegmentKeys == "HIGHWATCHES" || productSegmentKeys == "JEWEL" || string.IsNullOrEmpty(productSegmentKeys)) && productChannelKeys.Contains("web");
            bool webLeatherGoods = productLineKeys == "LEATHERGOODS" && string.IsNullOrEmpty(productSegmentKeys) && productChannelKeys.Contains("web");
            bool webEyeWear = productLineKeys == "EYEWEAR" && productChannelKeys.Contains("web");
            bool webObject = productLineKeys == "OBJECTS" && productChannelKeys == "web";
            bool webFragrance = productLineKeys == "FRAGRANCES" && productChannelKeys.Contains("web");

            if (webJewelry || webWatch || webLeatherGoods || webEyeWear || webObject || webFragrance)
            {
                return "Web";
            }

            return "";
        }

        private static void ExtractTricepsChannel(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = GetTricepsProductChannelValue(inRiverEntity)
            });
        }

        private static string GetTricepsProductChannelValue(Entity product)
        {
            var productLineKeys = ValueHelpers.GetCvlKeys(product.GetField("ProductLine").Data);
            var productSegmentKeys = ValueHelpers.GetCvlKeys(product.GetField("ProductSegment").Data);
            var productChannelKeys = ValueHelpers.GetCvlKeys(product.GetField("ProductChannel").Data);

            var webJewelry = productLineKeys == "JEWELRY" && (productSegmentKeys == "JEWEL" || productSegmentKeys == "NEWJEWELRY" || productSegmentKeys == "FINEJEWELRY") && productChannelKeys.Contains("web");
            var webWatch = productLineKeys == "WATCH" && (productSegmentKeys == "HIGHWATCHES" || productSegmentKeys == "JEWEL" || productSegmentKeys == "FINEJEWELRY" || string.IsNullOrEmpty(productSegmentKeys)) && productChannelKeys.Contains("web");
            var webLeatherGoods = productLineKeys == "LEATHERGOODS" && (productSegmentKeys == "FINEJEWELRY" || string.IsNullOrEmpty(productSegmentKeys)) && productChannelKeys.Contains("web");
            var webEyeWear = productLineKeys == "EYEWEAR" && productChannelKeys.Contains("web");
            var webObject = productLineKeys == "OBJECTS" && productChannelKeys == "web";
            var webFragrance = productLineKeys == "FRAGRANCES" && productChannelKeys.Contains("web");

            if (webJewelry || webWatch || webLeatherGoods || webEyeWear || webObject || webFragrance)
            {
                return "Web";
            }

            var hjJewelry = productLineKeys == "JEWELRY" &&
                            (productSegmentKeys == "FINEJEWELRYUS" || productSegmentKeys == "HIGHJEWELRYUS" ||
                             productSegmentKeys == "HIGHJEWELRY" || productSegmentKeys == "HIGHWATCHES" ||
                             string.IsNullOrEmpty(productSegmentKeys)) && productChannelKeys == "hj";
            var hjWatch = productLineKeys == "WATCH" && productSegmentKeys == "HIGHJEWELRY" && productChannelKeys == "hj";
            var hjLeatherGoods = productLineKeys == "LEATHERGOODS" && productSegmentKeys == "HIGHJEWELRY" && productChannelKeys == "hj";
            var hjEyeWear = productLineKeys == "EYEWEAR" && productSegmentKeys == "HIGHJEWELRY" && !string.IsNullOrEmpty(productChannelKeys);
            var hjObject = productLineKeys == "OBJECTS" && productSegmentKeys == "HIGHJEWELRY" && !string.IsNullOrEmpty(productChannelKeys);
            var hjFragrance = productLineKeys == "FRAGRANCES" && productSegmentKeys == "HIGHJEWELRY" && !string.IsNullOrEmpty(productChannelKeys);

            if (hjJewelry || hjWatch || hjLeatherGoods || hjEyeWear || hjObject || hjFragrance)
            {
                return "HJ";
            }

            return "";
        }
    }
}