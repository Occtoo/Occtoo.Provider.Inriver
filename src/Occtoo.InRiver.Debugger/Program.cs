using inRiver.Remoting;
using inRiver.Remoting.Cache;
using inRiver.Remoting.Objects;
using Occtoo.Generic.Debugger.Debuggers;
using Occtoo.Generic.Debugger.Loggers;
using System;
using System.Collections.Generic;

namespace Occtoo.Generic.Debugger
{
    internal class Program
    {
        private static void Main()
        {
            Console.WriteLine("Processing started...");

            TestEntityListener();

            TestSendToOcctooExtension();

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        private static void TestEntityListener(bool createDataSources = false)
        {
            var user = "you@users.com";
            var password = "";
            var environment = "test";
            var manager = RemoteManager.CreateInstance("https://remoting.productmarketingcloud.com", user, password, environment);
            var envId = new Guid("aae1371a-34cc-4965-a0ec-fb8dc75a9ac2");
            LoadCache(manager);

            var logger = new ConsoleLogger();
            var debugger = new ExportEntityListenerDebugger(manager, logger, envId);

            // Creating a EntityUpdate for entity with sys id 1
            debugger.Adapter.EntityUpdated(1, null);
        }

        private static void TestSendToOcctooExtension()
        {
            var user = "you@users.com";
            var password = "";
            var environment = "test";
            var manager = RemoteManager.CreateInstance("https://remoting.productmarketingcloud.com", user, password, environment);
            var envId = new Guid("aae1371a-34cc-4965-a0ec-fb8dc75a9ac2");
            LoadCache(manager);

            var logger = new ConsoleLogger();
            var debugger = new SendToOcctooDebugger(manager, logger, envId);

            // Run the scheduled extension to go through Connector events and send to Occtoo
            debugger.Adapter.Execute(false);
        }

        public static void LoadCache(RemoteManager manager)
        {
            CacheContainer cache = new CacheContainer();

            cache.SetLanguages(manager.UtilityService.GetAllLanguages());
            cache.SetCategories(manager.ModelService.GetAllCategories());
            cache.SetServerSettings(manager.UtilityService.GetAllServerSettings());
            cache.SetLinkTypes(manager.ModelService.GetAllLinkTypes());
            cache.SetFieldTypes(manager.ModelService.GetAllFieldTypes());
            cache.SetFieldSets(manager.ModelService.GetAllFieldSets());
            cache.SetEntityTypes(manager.ModelService.GetAllEntityTypes());
            cache.SetCVLs(manager.ModelService.GetAllCVLs());

            Dictionary<string, List<CVLValue>> dictionary = new Dictionary<string, List<CVLValue>>();

            foreach (var cvl in manager.ModelService.GetAllCVLs())
            {
                dictionary.Add(cvl.Id, manager.ModelService.GetCVLValuesForCVL(cvl.Id));
            }

            cache.SetCVLValues(dictionary);

            manager.SetCache(cache);
        }
    }
}