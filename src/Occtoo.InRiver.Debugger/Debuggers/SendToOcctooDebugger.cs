using inRiver.Remoting;
using inRiver.Remoting.Extension;
using Occtoo.Generic.Debugger.Settings;
using Occtoo.Generic.Infrastructure;
using Occtoo.Generic.Inriver;
using System;

namespace Occtoo.Generic.Debugger.Debuggers
{
    public class SendToOcctooDebugger
    {
        public readonly SendToOcctooExtension Adapter;

        public SendToOcctooDebugger(IinRiverManager inRiverManager,
            IExtensionLog logger, Guid environmentId)
        {
            var settings = AllExportSettings.GetSettings(environmentId);

            Adapter = new SendToOcctooExtension
            {
                Context = new inRiverContext(inRiverManager, logger)
                {
                    ExtensionId = "SendToOcctooDebugger",
                    Settings = SettingsHelper.AsSettingsDictionary(settings)
                }
            };

            Adapter.Test();
        }
    }
}