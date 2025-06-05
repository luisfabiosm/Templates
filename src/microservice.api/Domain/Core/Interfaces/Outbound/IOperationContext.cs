using System.Diagnostics;

namespace Domain.Core.Interfaces.Outbound
{
    public interface IOperationContext : IDisposable
    {
        Activity? ApiActivity { get; }
        void SetTag(string key, object value);
        void SetStatus(ActivityStatusCode status, string? description = null);
        void AddEvent(string name, object? data = null);
    }
}
