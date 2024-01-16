using inRiver.Remoting;
using inRiver.Remoting.Extension;
using Occtoo.Generic.Debugger.Settings;
using Occtoo.Generic.Debugger.Tests;
using Occtoo.Generic.Infrastructure;
using System;

namespace Occtoo.Generic.Debugger.Debuggers
{
    public class ExportEntityListenerDebugger
    {
        public readonly ExportTest Adapter;

        public ExportEntityListenerDebugger(IinRiverManager inRiverManager,
            IExtensionLog logger, Guid environmentId)
        {
            var settings = AllExportSettings.GetSettings(environmentId);

            Adapter = new ExportTest
            {
                Context = new inRiverContext(inRiverManager, logger)
                {
                    ExtensionId = "OcctooDebuggerEntityListenerExport",
                    Settings = SettingsHelper.AsSettingsDictionary(settings)
                }
            };

            Adapter.InitializeSettings(Adapter.Context);
            Adapter.Test();
        }
    }
}
