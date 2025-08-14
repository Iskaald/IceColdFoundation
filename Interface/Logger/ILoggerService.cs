namespace IceCold.Interface
{
    public interface ILoggerService : IIceColdService
    {
        public void Log(string message, string callerPath = "");
        public void LogWarning(string message, string callerPath = "");
        public void LogError(string message, string callerPath = "");
        public void LogException(System.Exception exception, string callerPath = "");
    }
}