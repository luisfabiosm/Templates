
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Interfaces.Outbound;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Adapters.Outbound.Logging
{
    /// <summary>
    /// Implementação de logging estruturado com alta performance
    /// Usando LoggerMessage para compilação otimizada
    /// Thread-safe e memory efficient
    /// </summary>
    public sealed class StructuredLoggingAdapter : ILoggingAdapter, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ActivitySource _activitySource;
        private readonly ConcurrentDictionary<string, object> _globalProperties;
        private readonly AsyncLocal<ConcurrentDictionary<string, object>> _scopedProperties;

        // Pre-compiled logger messages for better performance
        private static readonly Action<ILogger, string, Exception?> LogTraceMessage =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(1), "{Message}");

        private static readonly Action<ILogger, string, Exception?> LogDebugMessage =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(2), "{Message}");

        private static readonly Action<ILogger, string, Exception?> LogInformationMessage =
            LoggerMessage.Define<string>(LogLevel.Information, new EventId(3), "{Message}");

        private static readonly Action<ILogger, string, Exception?> LogWarningMessage =
            LoggerMessage.Define<string>(LogLevel.Warning, new EventId(4), "{Message}");

        private static readonly Action<ILogger, string, Exception?> LogErrorMessage =
            LoggerMessage.Define<string>(LogLevel.Error, new EventId(5), "{Message}");

        private static readonly Action<ILogger, string, Exception?> LogCriticalMessage =
            LoggerMessage.Define<string>(LogLevel.Critical, new EventId(6), "{Message}");

        public StructuredLoggingAdapter(ILogger logger, string serviceName)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activitySource = new ActivitySource(serviceName);
            _globalProperties = new ConcurrentDictionary<string, object>();
            _scopedProperties = new AsyncLocal<ConcurrentDictionary<string, object>>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogTrace(string message, params object[] args)
        {
            if (!_logger.IsEnabled(LogLevel.Trace)) return;

            LogTraceMessage(_logger, FormatMessage(message, args), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogDebug(string message, params object[] args)
        {
            if (!_logger.IsEnabled(LogLevel.Debug)) return;

            LogDebugMessage(_logger, FormatMessage(message, args), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogInformation(string message, params object[] args)
        {
            if (!_logger.IsEnabled(LogLevel.Information)) return;

            LogInformationMessage(_logger, FormatMessage(message, args), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogWarning(string message, params object[] args)
        {
            if (!_logger.IsEnabled(LogLevel.Warning)) return;

            LogWarningMessage(_logger, FormatMessage(message, args), null);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogError(string message, Exception? exception = null, params object[] args)
        {
            if (!_logger.IsEnabled(LogLevel.Error)) return;

            LogErrorMessage(_logger, FormatMessage(message, args), exception);

            // Add error information to current activity
            var currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                currentActivity.SetStatus(ActivityStatusCode.Error, message);
                currentActivity.SetTag("error", true);
                if (exception != null)
                {
                    currentActivity.SetTag("error.type", exception.GetType().Name);
                    currentActivity.SetTag("error.message", exception.Message);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void LogCritical(string message, Exception? exception = null, params object[] args)
        {
            LogCriticalMessage(_logger, FormatMessage(message, args), exception);

            // Always add critical errors to activity
            var currentActivity = Activity.Current;
            if (currentActivity != null)
            {
                currentActivity.SetStatus(ActivityStatusCode.Error, message);
                currentActivity.SetTag("error", true);
                currentActivity.SetTag("error.level", "critical");
                if (exception != null)
                {
                    currentActivity.SetTag("error.type", exception.GetType().Name);
                    currentActivity.SetTag("error.message", exception.Message);
                }
            }
        }

        public IOperationContext StartOperation(
            string operationName,
            string correlationId,
            ActivityContext parentContext = default)
        {
            var activity = _activitySource.StartActivity(
                operationName,
                ActivityKind.Internal,
                parentContext);

            if (activity != null)
            {
                activity.SetTag("correlation_id", correlationId);
                activity.SetTag("operation.name", operationName);

                // Add global properties to activity
                foreach (var property in _globalProperties)
                {
                    activity.SetTag($"global.{property.Key}", property.Value?.ToString());
                }
            }

            return new OperationContext(activity);
        }

        public void AddProperty(string key, object value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            // Add to scoped properties for current async context
            _scopedProperties.Value ??= new ConcurrentDictionary<string, object>();
            _scopedProperties.Value.TryAdd(key, value);

            // Also add to current activity if exists
            Activity.Current?.SetTag(key, value?.ToString());
        }

        public void AddGlobalProperty(string key, object value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);
            _globalProperties.TryAdd(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string FormatMessage(string message, params object[] args)
        {
            if (args.Length == 0) return message;

            try
            {
                return string.Format(message, args);
            }
            catch
            {
                // If formatting fails, return original message
                return message;
            }
        }

        public void Dispose()
        {
            _activitySource?.Dispose();
            _globalProperties?.Clear();
        }
    }

  
}

