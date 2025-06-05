using Domain.Core.Interfaces.Outbound;
using System.Diagnostics;

namespace Adapters.Outbound.Logging
{
    public class NoOpOperationContext: IOperationContext
    {
        public Activity ApiActivity { get; }

        public NoOpOperationContext(Activity activity)
        {
            ApiActivity = activity ?? Activity.Current;
        }

        public void Dispose()
        {
            ApiActivity?.Dispose();
        }

        public void SetTag(string key, string value) { }

        public void SetStatus(string status) { }

        public IOperationContext StartOperation(
           string operationName,
           string correlationId,
           ActivityContext parentContext = default,
           ActivityKind kind = ActivityKind.Internal)
        {
            return this;
        }
    }
}
