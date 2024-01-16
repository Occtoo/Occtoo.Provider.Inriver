using CSharpFunctionalExtensions;
using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Newtonsoft.Json;
using Occtoo.Generic.Inriver.Extractors.Interfaces;
using Occtoo.Generic.Inriver.Extractors.Model;
using Occtoo.Generic.Inriver.Helpers;
using Occtoo.Generic.Inriver.Model;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Entity = inRiver.Remoting.Objects.Entity;

namespace Occtoo.Generic.Inriver.Extractors
{
    public class ChildMediaIdExtractor : IExceptionFieldExtractor
    {
        private readonly Settings _settings;
        private readonly inRiverContext _context;
        private readonly OnboardingServiceClient _serviceClient;

        public ChildMediaIdExtractor(Settings settings,
            inRiverContext context)
        {
            _settings = settings;
            _context = context;
            _serviceClient = new OnboardingServiceClient(settings.OcctooDataProviderId, settings.OcctooDataProviderSecret);
        }

        public void Extract(DynamicEntity dynamicEntity, Entity inRiverEntity, ExceptionFieldSettings settings)
        {
            var extractSettings = ParseParams(settings.Params);
            if (extractSettings == null || !extractSettings.Valid()) return;

            var childLinks =
                _context.ExtensionManager.DataService.GetOutboundLinksForEntityAndLinkType(inRiverEntity.Id,
                    settings.Id);
            if (!childLinks.Any()) return;

            if (extractSettings.Order)
            {
                childLinks = childLinks.OrderBy(x => x.Index).ToList();
            }

            var children =
                _context.ExtensionManager.DataService.GetEntities(childLinks.Select(x => x.Target.Id).ToList(),
                    LoadLevel.DataOnly);

            var qualifiedChildren = GetQualifiedChildren(children, extractSettings);

            var child = extractSettings.First
                ? qualifiedChildren.FirstOrDefault()
                : qualifiedChildren.LastOrDefault();

            if (child == null) return;

            var id = ValueHelpers.GetMediaId(child, extractSettings.UniqueFieldIds, GetMediaServiceNamespace());

            if (string.IsNullOrEmpty(id)) return;

            dynamicEntity.Properties.Add(new DynamicProperty
            {
                Id = settings.Alias,
                Language = string.Empty,
                Value = id
            });
        }

        private List<Entity> GetQualifiedChildren(List<Entity> children, ChildMediaSettings settings)
        {
            var response = new List<Entity>();

            foreach (var primaryFieldValue in settings.PrimaryFieldValues)
            {
                var primaryChildren = children
                    .Where(c =>
                        c.GetField(settings.PrimaryFieldId)?.Data?.ToString() == primaryFieldValue)
                    .ToList();

                if (!primaryChildren.Any()) continue;

                if (!string.IsNullOrEmpty(settings.SecondaryFieldId))
                {
                    foreach (var secondaryFieldValue in settings.SecondaryFieldValues)
                    {
                        var secondaryChildren = primaryChildren
                            .Where(c =>
                                c.GetField(settings.SecondaryFieldId)?.Data?.ToString() == secondaryFieldValue)
                            .ToList();

                        if (!secondaryChildren.Any()) continue;

                        response.AddRange(secondaryChildren);
                        break;
                    }
                }

                if (response.Any()) break;

                if (!primaryChildren.Any()) continue;

                response.AddRange(primaryChildren);
                break;
            }

            return response;
        }

        public EntityMedia UploadOrGetFileInMediaService(string resourceUrl, string uniqueId, string filename)
        {
            try
            {
                var apiResult = _serviceClient.GetFileFromUniqueId(uniqueId);
                if (apiResult.StatusCode != 200 || string.IsNullOrEmpty(apiResult.Result.Id))
                {
                    var cancellationToken = new CancellationTokenSource(180000).Token; // 3 mins
                    apiResult = _serviceClient.UploadFromLink(new FileUploadFromLink(resourceUrl, filename) { UniqueIdentifier = uniqueId }, null, cancellationToken);
                    if (apiResult.StatusCode != 200)
                    {
                        _context.Log(LogLevel.Error, $"File upload failed: {resourceUrl}:{uniqueId}:{filename} ");
                        return default;
                    }
                }

                return new EntityMedia
                {
                    Key = apiResult.Result.Id,
                    Url = apiResult.Result.PublicUrl,
                    OriginalUrl = apiResult.Result.SourceUrl
                };
            }
            catch (Exception ex)
            {
                _context.Logger.Log(LogLevel.Debug,
                    $"Unable to upload file into media service. File: {resourceUrl}:{uniqueId}:{filename}. Message: {ex.Message}");
                return default;
            }
        }

        private static ChildMediaSettings ParseParams(string settingsParams)
        {
            if (string.IsNullOrEmpty(settingsParams)) return null;

            try
            {
                return JsonConvert.DeserializeObject<ChildMediaSettings>(settingsParams);
            }
            catch
            {
                return null;
            }
        }

        private string GetMediaServiceNamespace()
        {
            return _settings.SkipMediaKeyGuidTransform
                ? _settings.Environment
                : Constants.Occtoo.DefaultMediaNamespace;
        }
    }
}