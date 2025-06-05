using Domain.Core.Interfaces.Outbound;
using System.Diagnostics;



namespace Adapters.Outbound.Logging
{

    /// <summary>
    /// Context para operações com telemetria
    /// </summary>
    public sealed class OperationContext : IOperationContext
    {
        public Activity? ApiActivity { get; }

        public OperationContext(Activity? activity)
        {
            Activity = activity;
        }

        public void SetTag(string key, object value)
        {
            Activity?.SetTag(key, value?.ToString());
        }

        public void SetStatus(ActivityStatusCode status, string? description = null)
        {
            if (Activity == null) return;

            Activity.SetStatus(status, description);
        }

        public void AddEvent(string name, object? data = null)
        {
            if (Activity == null) return;

            var tags = data != null
                ? new ActivityTagsCollection { ["data"] = data.ToString() }
                : null;

            Activity.AddEvent(new ActivityEvent(name, DateTimeOffset.UtcNow, tags));
        }

        public void Dispose()
        {
            Activity?.Dispose();
        }
    }


}
