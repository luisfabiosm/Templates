using Adapters.Inbound.Middleware;
using Adapters.Inbound.WebApi.Sample.Endpoints;
using Adapters.Inbound.WebApi.Sample.Mapping;
using Microsoft.OpenApi.Models;

namespace Adapters.Inbound.Extensions
{
    public static class WebApiExtensions
    {
        public static IServiceCollection addWebApiEndpoints(this IServiceCollection services, IConfiguration configuration)
        {

            services.ConfigureHttpJsonOptions(options => {
                options.SerializerOptions.DefaultIgnoreCondition =
                    System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });
            services.AddScoped<MappingHttpRequestToTransaction>();
            services.AddEndpointsApiExplorer();
            services.AddHealthChecks();
            services.AddJwtAuthentication(configuration);

            return services;
        }


        public static IServiceCollection ConfigureSwagger(this IServiceCollection services, string apiName, string version = "v1")
        {
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Micorservice API",
                    Version = "v1",
                    Description = "API Modelo.",
                    Contact = new OpenApiContact
                    {
                        Name = "Backside Dev Team",
                        Email = "fabio.backside@gmail.com"
                    }
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Cabeçalho de autorização JWT usando o esquema Bearer. Exemplo: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                options.OperationFilter<SwaggerMinimalApiOperationFilter>();
            });

            return services;
        }

        public static void UseAPIExtensions(this WebApplication app)
        {

            if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName != "Production")
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseHttpHandlingMiddleware();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapHealthChecks("/health");

            app.AddSampleTaskEndpoints();

            app.Run();
        }
    }
}
