using System;
using TinyIoC;

namespace Occtoo.Generic.Infrastructure.Base
{
    public class DynamicStartup : IExtensionStartup
    {
        private readonly Action<TinyIoCContainer> _setup;

        public DynamicStartup(Action<TinyIoCContainer> setup)
        {
            _setup = setup;
        }

        public void ConfigureServices(TinyIoCContainer services)
        {
            _setup(services);
        }
    }
}
