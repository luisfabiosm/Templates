using Domain.Core.Interfaces.Outbound;
using Domain.Core.ResultPattern;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Adapters.Inbound.Middleware
{
    /// <summary>
    /// Middleware para tratamento global de exceções seguindo princípios Clean
    /// Converte exceções em responses HTTP apropriados
    /// </summary>
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILoggingAdapter _logger;
        private readonly bool _includeStackTrace;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILoggingAdapter logger,
            IHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _includeStackTrace = environment.IsDevelopment();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";

            _logger.LogError(
                "Unhandled exception occurred. CorrelationId: {CorrelationId}",
                exception,
                correlationId);

            var (statusCode, error) = MapExceptionToError(exception);
            var response = CreateErrorResponse(error, correlationId);

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private (HttpStatusCode statusCode, Error error) MapExceptionToError(Exception exception)
        {
            return exception switch
            {
                ArgumentException _ => (HttpStatusCode.BadRequest, Error.Validation(exception.Message)),
                ArgumentNullException _ => (HttpStatusCode.BadRequest, Error.Validation("Required parameter is missing")),
                InvalidOperationException _ => (HttpStatusCode.BadRequest, Error.Business(exception.Message)),
                UnauthorizedAccessException _ => (HttpStatusCode.Unauthorized, Error.Business("Access denied")),
                NotImplementedException _ => (HttpStatusCode.NotImplemented, Error.Internal("Feature not implemented")),
                TimeoutException _ => (HttpStatusCode.RequestTimeout, Error.Internal("Request timeout")),
                TaskCanceledException _ => (HttpStatusCode.RequestTimeout, Error.Internal("Request was cancelled")),
                _ => (HttpStatusCode.InternalServerError, Error.Internal("An internal server error occurred"))
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ErrorResponse CreateErrorResponse(Error error, string correlationId)
        {
            return new ErrorResponse
            {
                Error = new ErrorDetail
                {
                    Code = error.Code.Value,
                    Message = error.Message.Value,
                    Type = error.Type.ToString(),
                    CorrelationId = correlationId,
                    Timestamp = DateTime.UtcNow
                },
                Success = false
            };
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private sealed record ErrorResponse
        {
            public bool Success { get; init; }
            public ErrorDetail Error { get; init; } = default!;
        }

        private sealed record ErrorDetail
        {
            public string Code { get; init; } = string.Empty;
            public string Message { get; init; } = string.Empty;
            public string Type { get; init; } = string.Empty;
            public string CorrelationId { get; init; } = string.Empty;
            public DateTime Timestamp { get; init; }
        }
    }
}