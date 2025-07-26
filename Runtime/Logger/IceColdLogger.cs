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

        public static void Log(string message)
        {
            service?.Log(message);
        }

        public static void LogWarning(string message)
        {
            service?.LogWarning(message);
        }
        
        public static void LogError(string message)
        {
            service?.LogError(message);
        }
        
        public static void LogException(System.Exception exception)
        {
            service?.LogError(exception.ToString());
        }
    }
}