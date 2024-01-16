using Occtoo.Generic.Debugger.Settings.Clients;
using System;
using System.Collections.Generic;

namespace Occtoo.Generic.Debugger.Settings
{
    public static class AllExportSettings
    {
        public static Inriver.Settings GetSettings(Guid environmentId)
        {
            return SettingsList[environmentId];
        }

        private static Dictionary<Guid, Inriver.Settings> SettingsList { get; set; } =
            new Dictionary<Guid, Inriver.Settings>
            {
                {
                    // Example Client
                    new Guid("aae1371a-34cc-4965-a0ec-fb8dc75a9ac2"),
                    ClientSettingsExample.Create()
                }
            };
    }
}