using Adapters.Inbound.Middleware;
//using Adapters.Inbound.WebApi.Extensions;
//using Adapters.Inbound.WebApi.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Configurations
{
    /// <summary>
    /// Configuração de adaptadores Web API seguindo SRP
    /// </summary>
    public static class WebApiConfiguration
    {
        public static IServiceCollection AddWebApiAdapters(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configuração JSON otimizada
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = null;
                options.SerializerOptions.WriteIndented = false;
                options.SerializerOptions.DefaultIgnoreCondition =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
            });

            // Endpoints API Explorer para Swagger
            services.AddEndpointsApiExplorer();

#if UseSwagger
            services.AddSwaggerServices();
#endif

#if UseJwtAuth
            services.AddJwtAuthentication(configuration);
#endif

#if UseHealthChecks
            services.AddHealthCheckServices(configuration);
#endif

            // Middleware personalizado
            services.AddTransient<ExceptionHandlingMiddleware>();
            services.AddTransient<PerformanceMiddleware>();
            services.AddTransient<CorrelationIdMiddleware>();

            return services;
        }

        public static WebApplication UseWebApiPipeline(this WebApplication app)
        {
            // Middleware pipeline otimizado
            if (app.Environment.IsDevelopment())
            {
#if UseSwagger
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Clean Architecture API v1");
                    options.RoutePrefix = "swagger";
                });
#endif
            }

            // Middleware de segurança
            app.UseHttpsRedirection();
            app.UseHsts();

            // Middleware customizado
            app.UseMiddleware<CorrelationIdMiddleware>();
            app.UseMiddleware<PerformanceMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

#if UseJwtAuth
            app.UseAuthentication();
            app.UseAuthorization();
#endif

#if UseHealthChecks
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/health/ready");
            app.MapHealthChecks("/health/live");
#endif

            // Registro de endpoints
            app.MapApiEndpoints();

            return app;
        }
    }
}