using Domain.Core.Interfaces.Outbound;
using Domain.Core.Settings;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Reflection;


namespace Adapters.Outbound.Logging
{
    public static class LoggingExtensions
    {
        public static IServiceCollection AddOptimizedLogging(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Configure Serilog with structured logging
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", Assembly.GetExecutingAssembly().GetName().Name)
                .Enrich.WithProperty("ServiceVersion", Assembly.GetExecutingAssembly().GetName().Version?.ToString())
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .CreateLogger();

            // Configure Microsoft.Extensions.Logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(Log.Logger, dispose: true);
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Register structured logging adapter
            services.AddSingleton<ILoggingAdapter>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<StructuredLoggingAdapter>>();
                var serviceName = Assembly.GetExecutingAssembly().GetName().Name ?? "microservice-api";
                return new StructuredLoggingAdapter(logger, serviceName);
            });

            // Configure OpenTelemetry
            var resourceBuilder = ResourceBuilder.CreateDefault()
                .AddService(
                    serviceName: Assembly.GetExecutingAssembly().GetName().Name ?? "microservice-api",
                    serviceVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0");

            services.AddOpenTelemetry()
                .WithTracing(tracing =>
                {
                    tracing
                        .SetResourceBuilder(resourceBuilder)
                        .AddSource(Assembly.GetExecutingAssembly().GetName().Name)
                        .AddAspNetCoreInstrumentation(options =>
                        {
                            options.RecordException = true;
                            options.EnrichWithHttpRequest = (activity, request) =>
                            {
                                activity.SetTag("http.request.header.user-agent", request.Headers["User-Agent"].ToString());
                            };
                            options.EnrichWithHttpResponse = (activity, response) =>
                            {
                                activity.SetTag("http.response.status_code", response.StatusCode);
                            };
                        })
                        .AddHttpClientInstrumentation()
                        .SetSampler(new TraceIdRatioBasedSampler(1.0))
                        .AddConsoleExporter();

                    // Add OTLP exporter if configured
                    var otlpEndpoint = configuration["AppSettings:Otlp:Endpoint"];
                    if (!string.IsNullOrEmpty(otlpEndpoint))
                    {
                        tracing.AddOtlpExporter(options =>
                        {
                            options.Endpoint = new Uri(otlpEndpoint);
                        });
                    }
                });

            return services;
        }
    }
}
