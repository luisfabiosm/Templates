using System.Diagnostics;

namespace Domain.Core.Interfaces.Outbound
{
    public interface ILoggingAdapter
    {
        void LogTrace(string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, Exception? exception = null, params object[] args);
        void LogCritical(string message, Exception? exception = null, params object[] args);

        IOperationContext StartOperation(string operationName, string correlationId, ActivityContext parentContext = default);
        void AddProperty(string key, object value);
    }
}
