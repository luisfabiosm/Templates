using Microsoft.Extensions.DependencyInjection;

namespace Configurations
{
    /// <summary>
    /// Configuração de cache seguindo SRP
    /// </summary>
    public static class CacheConfiguration
    {
        public static IServiceCollection AddCacheAdapters(
            this IServiceCollection services,
            IConfiguration configuration)
        {
#if RedisCondition
            services.AddRedisCache(configuration);
#endif

            return services;
        }

#if RedisCondition
        private static IServiceCollection AddRedisCache(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                options.InstanceName = configuration["Redis:InstanceName"] ?? "microservice";
            });

            services.AddSingleton<ICacheAdapter, RedisCacheAdapter>();

            return services;
        }
#endif
    }
}