// Adapters/Inbound/WebApi/Middleware/PerformanceMiddleware.cs
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;
using Domain.Core.Interfaces.Outbound;

namespace Adapters.Inbound.Middleware
{
    /// <summary>
    /// Middleware para monitoramento de performance seguindo SRP
    /// Coleta métricas sem impactar significativamente a performance
    /// </summary>
    public sealed class PerformanceMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggingAdapter _logger;
        private readonly IServiceProvider _serviceProvider;

        // Thresholds para logging diferenciado
        private const long SlowRequestThresholdMs = 1000;
        private const long VerySlowRequestThresholdMs = 5000;

        public PerformanceMiddleware(
            RequestDelegate next,
            ILoggingAdapter logger,
            IServiceProvider serviceProvider)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task InvokeAsync(HttpContext context)
        {
            using var activity = StartActivity(context);
            var stopwatch = Stopwatch.StartNew();
            var memoryBefore = GC.GetTotalMemory(false);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var memoryAfter = GC.GetTotalMemory(false);

                LogPerformanceMetrics(context, stopwatch.ElapsedMilliseconds, memoryBefore, memoryAfter);
                RecordMetrics(context, stopwatch.ElapsedMilliseconds);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Activity? StartActivity(HttpContext context)
        {
            var activitySource = _serviceProvider.GetService<ActivitySource>();
            return activitySource?.StartActivity($"{context.Request.Method} {context.Request.Path}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LogPerformanceMetrics(
            HttpContext context,
            long elapsedMs,
            long memoryBefore,
            long memoryAfter)
        {
            var memoryUsed = memoryAfter - memoryBefore;
            var request = context.Request;
            var response = context.Response;

            if (elapsedMs >= VerySlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "VERY SLOW REQUEST: {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}. Memory used: {MemoryUsed} bytes",
                    request.Method,
                    request.Path,
                    elapsedMs,
                    response.StatusCode,
                    memoryUsed);
            }
            else if (elapsedMs >= SlowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "SLOW REQUEST: {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                    request.Method,
                    request.Path,
                    elapsedMs,
                    response.StatusCode);
            }
            else
            {
                _logger.LogDebug(
                    "REQUEST: {Method} {Path} completed in {ElapsedMs}ms with status {StatusCode}",
                    request.Method,
                    request.Path,
                    elapsedMs,
                    response.StatusCode);
            }

            // Adicionar métricas à atividade atual
            Activity.Current?.SetTag("http.request.duration_ms", elapsedMs);
            Activity.Current?.SetTag("http.request.memory_used", memoryUsed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecordMetrics(HttpContext context, long elapsedMs)
        {
            // Registrar métricas usando OpenTelemetry se disponível
            var metricsRecorder = _serviceProvider.GetService<IMetricsRecorder>();
            if (metricsRecorder != null)
            {
                var endpoint = $"{context.Request.Method} {context.Request.Path}";
                metricsRecorder.RecordRequestDuration(elapsedMs / 1000.0, endpoint);
                metricsRecorder.RecordRequestCount(endpoint, context.Response.StatusCode);
            }
        }
    }
}

