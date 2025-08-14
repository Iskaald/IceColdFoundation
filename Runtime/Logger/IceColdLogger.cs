using System.Runtime.CompilerServices;
using IceCold.Interface;

namespace IceCold
{
    public static class IceColdLogger
    {
        private static ILoggerService service;

        public static void Init(ILoggerService loggerService)
        {
            service = loggerService;
        }

        public static void Log(string message, [CallerFilePath] string callerPath = "")
        {
            service?.Log(message, callerPath);
        }

        public static void LogWarning(string message, [CallerFilePath] string callerPath = "")
        {
            service?.LogWarning(message, callerPath);
        }
        
        public static void LogError(string message, [CallerFilePath] string callerPath = "")
        {
            service?.LogError(message, callerPath);
        }
        
        public static void LogException(System.Exception exception, [CallerFilePath] string callerPath = "")
        {
            service?.LogException(exception, callerPath);
        }
    }
}