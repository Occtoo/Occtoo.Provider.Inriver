using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using Occtoo.Generic.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using TinyIoC;

namespace Occtoo.Generic.Infrastructure.Base
{
    public class ExtensionInitialization<TSettings>
        where TSettings : class, new()
    {
        private readonly IExtensionStartup _startup;
        private Dictionary<string, string> _previousSettings;
        private TinyIoCContainer _container;

        public ExtensionInitialization(IExtensionStartup startup)
        {
            _startup = startup;
        }

        public TService GetService<TService>(inRiverContext inRiverContext)
            where TService : class
        {
            if (inRiverContext == null) throw new ArgumentNullException(nameof(inRiverContext));
            try
            {
                if (ConfigurationHasBeenModified(inRiverContext, _previousSettings))
                {
                    inRiverContext.Logger?.Log(LogLevel.Information, "Configuration has been modified. Re-initializing components.");
                    _container = RebuildIoCContainer(inRiverContext);
                    _previousSettings = inRiverContext.Settings;
                    inRiverContext.Logger?.Log(LogLevel.Information, "Initialization successful!");
                }
                var service = _container.Resolve<TService>();
                if (service == null)
                {
                    throw new TinyIoCResolutionException(typeof(TService));
                }

                return service;
            }
            catch (Exception e)
            {
                inRiverContext.Logger?.Log(LogLevel.Error, "Failed to initialize: " + e.Message);
                inRiverContext.Logger?.Log(LogLevel.Error, $"Failed initialization details. Exception: {e}");
                throw;
            }
        }

        private TinyIoCContainer RebuildIoCContainer(inRiverContext inRiverContext)
        {
            var container = new TinyIoCContainer();
            var settings = inRiverContext.GetSettingsAs<TSettings>();

            container.Register((ioc, npo) => inRiverContext);
            container.Register((ioc, npo) => inRiverContext.Logger);
            container.Register(settings);

            _startup.ConfigureServices(container);

            return container;
        }

        private static bool ConfigurationHasBeenModified(inRiverContext context, Dictionary<string, string> previousSettings)
        {
            return !context.Settings.DictionaryEqual(previousSettings);
        }
    }
}