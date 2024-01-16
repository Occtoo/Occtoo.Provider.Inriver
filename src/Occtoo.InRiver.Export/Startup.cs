using Occtoo.Generic.Infrastructure.Base;
using Occtoo.Generic.Inriver.Extractors.Factory;
using Occtoo.Generic.Inriver.Services;
using TinyIoC;

namespace Occtoo.Generic.Inriver
{
    public class Startup : IExtensionStartup
    {
        public void ConfigureServices(TinyIoCContainer services)
        {
            services.Register<IExtractorsFactory, ExtractorsFactory>().AsSingleton();
            services.Register<IDocumentsService, DocumentsService>().AsSingleton();
            services.Register<IMediaService, MediaService>().AsSingleton();
            services.Register<IEntitiesService, EntitiesService>().AsSingleton();
            services.Register<IExporterService, ExporterService>().AsSingleton();
        }
    }
}