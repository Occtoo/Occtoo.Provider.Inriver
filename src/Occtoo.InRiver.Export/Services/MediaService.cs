using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Inriver.Helpers;
using Occtoo.Generic.Inriver.Model;
using Occtoo.Generic.Inriver.Model.Settings;
using Occtoo.Onboarding.Sdk;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Occtoo.Generic.Inriver.Services
{
    public interface IMediaService
    {
        void SendDocuments(Entity entity, EntitySettings settings, List<DynamicEntity> documents, string entitySystemIdAlias);
    }

    public class MediaService : IMediaService
    {
        private readonly inRiverContext _context;
        private readonly Settings _settings;
        private readonly IDocumentsService _documentsService;
        private readonly OnboardingServiceClient _serviceClient;

        public MediaService(inRiverContext context,
            Settings settings,
            IDocumentsService documentsService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _documentsService = documentsService ?? throw new ArgumentNullException(nameof(documentsService));
            _serviceClient = new OnboardingServiceClient(settings.OcctooDataProviderId, settings.OcctooDataProviderSecret);
        }

        public void SendDocuments(Entity entity, EntitySettings settings, List<DynamicEntity> documents, string entitySystemIdAlias)
        {
            foreach (var document in documents)
            {
                if (!ValueHelpers.GetResourceFilename(entity, _settings.DocumentIdForbiddenChars, out var filename)) continue;

                if (document.Delete)
                {
                    DeleteFileFromMediaService(document.Key);
                }
                else
                {
                    EntityMedia occtooMedia;
                    if (_settings.SkipMediaKeyGuidTransform)
                    {
                        var resourceFileId = document.Properties.FirstOrDefault(x => x.Id == "ResourceFileId")?.Value ?? "";
                        var uniqueKey = resourceFileId == "" ? document.Key : $"{resourceFileId}_{filename}";

                        occtooMedia = UploadOrGetFileInMediaService(entity.MainPictureUrl, uniqueKey, filename);
                    }
                    else
                        occtooMedia = UploadOrGetFileInMediaService(entity.MainPictureUrl, document.Key, filename);

                    if (occtooMedia == null) continue;

                    document.Properties.Add(new DynamicProperty
                    { Id = "Url", Value = occtooMedia.Url, Language = string.Empty });
                    document.Properties.Add(new DynamicProperty
                    { Id = "OriginalUrl", Value = occtooMedia.OriginalUrl, Language = string.Empty });
                }

                _documentsService.SendDocuments(settings.DataSource, new List<DynamicEntity> { document }, entitySystemIdAlias);
            }
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
                        _context.Log(inRiver.Remoting.Log.LogLevel.Error, $"File upload failed: {resourceUrl}:{uniqueId}:{filename} ");
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

        private void DeleteFileFromMediaService(string uniqueId)
        {
            try
            {
                var apiResult = _serviceClient.GetFileFromUniqueId(uniqueId);
                if (apiResult.StatusCode == 200 && !string.IsNullOrEmpty(apiResult.Result?.Id))
                {
                    var result = _serviceClient.DeleteFile(apiResult.Result.Id);
                    _context.Log(LogLevel.Debug,
                        $"Delete resource from media service {uniqueId} -> Status: {result.StatusCode}");
                }
                else
                {
                    _context.Log(LogLevel.Debug, $"Resource {uniqueId} was not found in the media service!");
                }
            }
            catch (Exception ex)
            {
                _context.Logger.Log(LogLevel.Debug, $"Unable to delete resource from media service: {uniqueId}. Message: {ex.Message}");
            }
        }
    }
}