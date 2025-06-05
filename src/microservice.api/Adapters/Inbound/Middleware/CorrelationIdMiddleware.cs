using Domain.Core.Interfaces.Outbound;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Adapters.Inbound.Middleware
{
    /// <summary>
    /// Middleware para gestão de Correlation ID seguindo princípios de rastreabilidade
    /// Implementação otimizada para alta performance
    /// </summary>
    public sealed class CorrelationIdMiddleware
    {
        private readonly RequestDelegate _next;
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private const string CorrelationIdKey = "CorrelationId";

        public CorrelationIdMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Task InvokeAsync(HttpContext context)
        {
            var correlationId = GetOrCreateCorrelationId(context);

            // Adicionar ao contexto HTTP para uso posterior
            context.Items[CorrelationIdKey] = correlationId;

            // Adicionar ao cabeçalho de resposta
            context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);

            // Adicionar à atividade atual para rastreamento
            Activity.Current?.SetTag("correlation_id", correlationId);

            // Adicionar ao contexto de log
            using var scope = context.RequestServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();

            var logger = scope.ServiceProvider.GetService<ILoggingAdapter>();
            logger?.AddProperty("CorrelationId", correlationId);

            await _next(context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetOrCreateCorrelationId(HttpContext context)
        {
            // Tentar obter do cabeçalho da requisição
            if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue) &&
                !string.IsNullOrWhiteSpace(headerValue))
            {
                return headerValue.ToString();
            }

            // Gerar novo correlation ID se não existir
            return Guid.NewGuid().ToString("N")[..12]; // Formato compacto para performance
        }
    }
}