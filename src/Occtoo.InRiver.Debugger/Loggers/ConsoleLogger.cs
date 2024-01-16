using inRiver.Remoting.Extension;
using inRiver.Remoting.Log;
using System;

namespace Occtoo.Generic.Debugger.Loggers
{
    public class ConsoleLogger : IExtensionLog
    {
        public void Log(LogLevel level, string message)
        {
            Console.WriteLine($"{level} - {message}");
        }

        public void Log(LogLevel level, string message, Exception ex)
        {
            Console.WriteLine($"{level} - {message}.\n{ex}");
        }
    }
}