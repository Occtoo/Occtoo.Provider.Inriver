using TinyIoC;

namespace Occtoo.Generic.Infrastructure.Base
{
    public interface IExtensionStartup
    {
        void ConfigureServices(TinyIoCContainer services);
    }
}