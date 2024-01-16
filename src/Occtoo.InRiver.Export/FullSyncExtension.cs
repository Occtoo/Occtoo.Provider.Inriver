using inRiver.Remoting.Extension;
using inRiver.Remoting.Extension.Interface;
using inRiver.Remoting.Log;
using Newtonsoft.Json;
using Occtoo.Generic.Infrastructure;
using Occtoo.Generic.Infrastructure.Base;
using Occtoo.Generic.Inriver;
using Occtoo.Generic.Inriver.Services;
using System;
using System.Collections.Generic;

namespace Occtoo.InRiver.Export
{
    internal class FullSyncExtension : IScheduledExtension
    {
        #region init

        private readonly ExtensionInitialization<Settings> _initialization;
        private readonly JsonSerializerSettings _serializingSettings;
        public FullSyncExtension() : this(new Startup())
        {
        }

        public FullSyncExtension(IExtensionStartup startup)
        {
            try
            {
                _initialization = new ExtensionInitialization<Settings>(startup);
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.ToString());
            }

            try
            {
                _serializingSettings = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Serialize };
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, ex.ToString());
            }
        }

        public inRiverContext Context { get; set; }

        public Dictionary<string, string> DefaultSettings { get; } = SettingsHelper.AsDefaultSettings<Settings>();

        public string Test()
        {
            Context.Log(LogLevel.Information, "Test function run");
            return $"Extension {Context.ExtensionId} loaded correctly";
        }

        #endregion init

        public void Execute(bool force)
        {
            if (!force)
            {
                return;
            }

            try
            {
                var service = _initialization.GetService<IExporterService>(Context);
                service.FullExport();
            }
            catch (Exception ex)
            {
                Context.Log(LogLevel.Error, "Occtoo Export - error while performing full export.", ex);
                throw;
            }
        }
    }
}
