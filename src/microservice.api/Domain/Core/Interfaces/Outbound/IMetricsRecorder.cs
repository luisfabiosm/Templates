namespace Domain.Core.Interfaces.Outbound
{
    /// <summary>
    /// Interface para gravação de métricas seguindo ISP
    /// </summary>
    public interface IMetricsRecorder
    {
        void RecordRequestDuration(double durationSeconds, string endpoint);
        void RecordRequestCount(string endpoint, int statusCode);
        void RecordGaugeValue(string name, double value, params (string key, object value)[] tags);
        void IncrementCounter(string name, params (string key, object value)[] tags);
    }
}