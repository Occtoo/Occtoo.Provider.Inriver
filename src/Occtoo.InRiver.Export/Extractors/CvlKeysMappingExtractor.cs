using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Helpers;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class CvlKeysMappingExtractor : IExceptionFieldExtractor
    {
        private const char ListItemSeparator = '|';
        private const char MappingSeparator = ';';
        private const char ValuesSeparator = ':';

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var mappings = ParseParams(settings.Params);
            if (!mappings.Any()) return;

            var cvlKeys = ValueHelpers.GetCvlKeys(inRiverEntity.GetField(settings.Id)?.Data);
            if (string.IsNullOrEmpty(cvlKeys)) return;

            var newValues = (
                from cvlKey in cvlKeys.Split(new[] { Constants.Occtoo.MultiValueDefaultSeparator }, StringSplitOptions.RemoveEmptyEntries)
                let cvlMapping = mappings.FirstOrDefault(m => m.InRiverValues.Contains(cvlKey))
                select cvlMapping == null ? cvlKey : cvlMapping.OnboardingValue
                ).Distinct().ToList();

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = string.Join(Constants.Occtoo.MultiValueDefaultSeparator, newValues)
            });
        }

        private static List<CvlMapping> ParseParams(string settingsParams)
        {
            var response = new List<CvlMapping>();
            if (string.IsNullOrEmpty(settingsParams)) return response;

            response.AddRange(
                from items in settingsParams.Split(ListItemSeparator)
                select items.Split(MappingSeparator) into mappingParts
                where mappingParts.Length == 2
                let values = mappingParts[1].Split(ValuesSeparator)
                where values.Any()
                select new CvlMapping
                {
                    OnboardingValue = mappingParts[0],
                    InRiverValues = values.ToList()
                });

            return response;
        }

        private class CvlMapping
        {
            public CvlMapping()
            {
                InRiverValues = new List<string>();
            }

            public string OnboardingValue { get; set; }
            public List<string> InRiverValues { get; set; }
        }
    }
}