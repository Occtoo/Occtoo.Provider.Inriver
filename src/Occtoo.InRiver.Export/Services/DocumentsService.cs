using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using Occtoo.Onboarding.Sdk;
using Occtoo.Onboarding.Sdk.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Occtoo.Generic.Inriver.Services
{
    public interface IDocumentsService
    {
        IEnumerable<int> SendDocuments(string dataSource, List<DynamicEntity> entities, string entitySystemIdAlias);
    }

    public class DocumentsService : IDocumentsService
    {
        private readonly inRiverContext _context;
        private readonly Guid _correlationId;
        private readonly IOnboardingServiceClient _serviceClient;

        public DocumentsService(inRiverContext context,
            Settings settings)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _correlationId = Guid.NewGuid();
            _serviceClient = new OnboardingServiceClient(settings.OcctooDataProviderId, settings.OcctooDataProviderSecret);
        }

        public IEnumerable<int> SendDocuments(string dataSource, List<DynamicEntity> entities, string entitySystemIdAlias)
        {
            try
            {
                var response = _serviceClient.StartEntityImport(dataSource, entities, null, _correlationId);
                _context.Log(LogLevel.Debug, $"Import data into datasource {dataSource} -> Successful: {response.StatusCode == 202}");

                var idsAndKeys = entities.Select(x =>
                    $"{x.Properties.FirstOrDefault(y => y.Id == entitySystemIdAlias)?.Value ?? "N/A"} - {x.Key}");
                _context.Log(LogLevel.Debug, $"Imported keys into datasource {dataSource} -> ids and keys: {string.Join(", ", idsAndKeys)}");
                return new List<int>();
            }
            catch (Exception ex)
            {
                //Add logic to requeue entities
                _context.Logger.Log(LogLevel.Debug, $"Error when sending datasource: {dataSource}. Message: {ex.Message}.");
                return entities.Select(x => int.Parse(x.Properties.FirstOrDefault(y => y.Id == entitySystemIdAlias)?.Value));
            }
        }
    }
}