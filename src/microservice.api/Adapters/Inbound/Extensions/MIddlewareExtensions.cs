using Adapters.Inbound.Middleware;
using Microsoft.AspNetCore.Builder;

namespace Adapters.Inbound.Extensions
{
    /// <summary>
    /// Extensions para registrar middleware seguindo princípio DRY
    /// </summary>
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder app)
        {
            return app.UseMiddleware<PerformanceMiddleware>();
        }

        public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CorrelationIdMiddleware>();
        }

        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<ExceptionHandlingMiddleware>();
        }

        public static IApplicationBuilder UseCleanArchitectureMiddleware(this IApplicationBuilder app)
        {
            return app
                .UseCorrelationId()
                .UsePerformanceMonitoring()
                .UseGlobalExceptionHandling();
        }
    }
}