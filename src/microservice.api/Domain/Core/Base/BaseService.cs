using Adapters.Outbound.Metrics;
using Domain.Core.Exceptions;
using Domain.Core.Ports.Outbound;
using System;

namespace Domain.Core.Base
{
    public class BaseService
    {

        protected readonly ILoggingAdapter _loggingAdapter;
        protected readonly MetricsAdapter _metricsAdapter;

        public BaseService(IServiceProvider serviceProvider)
        {
            _loggingAdapter = serviceProvider.GetRequiredService<ILoggingAdapter>();
            _metricsAdapter = serviceProvider.GetRequiredService<MetricsAdapter>();
        }


        protected Exception HandleException(string methodName, Exception exception)
        {
            // Log estruturado da exceção
            _loggingAdapter.LogError(
                "Erro em {MethodName}: {ErrorMessage}",
                exception,
                methodName,
                exception.Message);

            // Determina se é uma exceção conhecida ou se deve ser encapsulada
            return IsKnownException(exception) ? exception : UnknownException(exception);
        }

        private static bool IsKnownException(Exception exception)
        {
            return exception is BusinessException or
                   InternalException or
                   ValidateException;
        }

        private static Exception UnknownException(Exception exception)
        {
            return new InternalException(
                exception.Message ?? "Erro interno não esperado",
                1,
                exception);
        }

        protected void RecordRequest(string endpoint)
        {
            _metricsAdapter.RecordRequest(endpoint);
        }

        protected void RecordRequestDuration(double duration, string endpoint)
        {
            _metricsAdapter.RecordRequestDuration(duration, endpoint);
        }

        protected IOperationContext StartOperation(string operationName, string correlationId)
        {
            _loggingAdapter.LogDebug("Iniciando operação: {OperationName} [CorrelationId: {CorrelationId}]",
                operationName, correlationId);

            RecordRequest(operationName);
            return _loggingAdapter.StartOperation(operationName, correlationId);
        }

        protected void AddTraceProperty(string key, string value)
        {
            _loggingAdapter.AddProperty(key, value);
        }

        protected void LogInformation(string message, params object[] args)
        {
            _loggingAdapter.LogInformation(message, args);
        }

        protected void LogWarning(string message, params object[] args)
        {
            _loggingAdapter.LogWarning(message, args);
        }
        protected void LogDebug(string message, params object[] args)
        {
            _loggingAdapter.LogDebug(message, args);
        }
        protected void LogError(string message, Exception ex = null, params object[] args)
        {
            _loggingAdapter.LogError(message, ex, args);
        }
    }
}
