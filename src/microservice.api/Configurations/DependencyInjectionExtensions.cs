// Configurations/DependencyInjectionExtensions.cs
using Adapters.Inbound.Middleware;
using Adapters.Outbound.Logging;
using Domain.Core.Base;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

namespace Configurations
{
    /// <summary>
    /// Extensões para configuração de injeção de dependência seguindo SOLID
    /// Princípio da Responsabilidade Única para cada tipo de configuração
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCleanArchitectureServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            return services
                .AddCoreServices()
                .AddMediatorServices()
                .AddValidationServices()
                .AddInboundAdapters(configuration)
                .AddOutboundAdapters(configuration)
                .AddDomainServices()
                .AddPerformanceOptimizations();
        }

        private static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            // Configurações básicas com lifetime otimizado
            services.TryAddSingleton<IServiceScopeFactory, DefaultServiceScopeFactory>();

            return services;
        }

        private static IServiceCollection AddMediatorServices(this IServiceCollection services)
        {
            // Mediator como Singleton para melhor performance
            services.TryAddSingleton<IBSMediator, BSMediator>();

            // Registro automático de handlers usando Reflection otimizada
            RegisterRequestHandlers(services);

            return services;
        }

        private static IServiceCollection AddValidationServices(this IServiceCollection services)
        {
            // Registro automático de validadores
            RegisterValidators(services);

            return services;
        }

        private static IServiceCollection AddInboundAdapters(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddWebApiAdapters(configuration);

            return services;
        }

        private static IServiceCollection AddOutboundAdapters(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services
                .AddLoggingAdapters(configuration)
                .AddDatabaseAdapters(configuration)
                .AddMessagingAdapters(configuration)
                .AddCacheAdapters(configuration);

#if UseMetrics
            services.AddMetricsAdapters(configuration);
#endif

            return services;
        }

        private static IServiceCollection AddDomainServices(this IServiceCollection services)
        {
            // Registro automático de domain services
            RegisterDomainServices(services);

            return services;
        }

        private static IServiceCollection AddPerformanceOptimizations(this IServiceCollection services)
        {
            // Object pooling para objetos caros de criar
            services.AddObjectPool();

            // Memory cache otimizado
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1000;
                options.CompactionPercentage = 0.25;
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
            });

            // HTTP client factory com pool de conexões
            services.AddHttpClient();

            return services;
        }

        private static void RegisterRequestHandlers(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var handlerTypes = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(type => type.GetInterfaces()
                    .Any(i => i.IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IBSRequestHandler<,>)))
                .ToArray();

            foreach (var handlerType in handlerTypes)
            {
                var handlerInterface = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IBSRequestHandler<,>));

                // Registro como Scoped para isolamento por request
                services.TryAddScoped(handlerInterface, handlerType);
            }
        }

        private static void RegisterValidators(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var validatorTypes = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(type => type.GetInterfaces()
                    .Any(i => i.IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IValidator<>)))
                .ToArray();

            foreach (var validatorType in validatorTypes)
            {
                var validatorInterface = validatorType.GetInterfaces()
                    .First(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IValidator<>));

                // Validadores como Singleton pois são stateless
                services.TryAddSingleton(validatorInterface, validatorType);
            }
        }

        private static void RegisterDomainServices(IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var domainServiceTypes = assembly.GetTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(type => type.Namespace?.Contains("Domain.Services") == true)
                .ToArray();

            foreach (var serviceType in domainServiceTypes)
            {
                var serviceInterfaces = serviceType.GetInterfaces()
                    .Where(i => i.Namespace?.Contains("Domain.Core.Interfaces") == true);

                foreach (var serviceInterface in serviceInterfaces)
                {
                    // Domain services como Scoped
                    services.TryAddScoped(serviceInterface, serviceType);
                }
            }
        }
    }
}

