using Microsoft.Extensions.DependencyInjection;

namespace Configurations
{
    /// <summary>
    /// Configuração de adaptadores de banco de dados seguindo OCP
    /// </summary>
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabaseAdapters(
            this IServiceCollection services,
            IConfiguration configuration)
        {
#if SqlServerCondition || PostgreSQLCondition
            services.AddSqlDatabaseAdapters(configuration);
#elif MongoDbCondition
            services.AddMongoDbAdapters(configuration);
#endif

            return services;
        }

#if SqlServerCondition || PostgreSQLCondition
        private static IServiceCollection AddSqlDatabaseAdapters(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Configuração de settings
            services.Configure<DBSettings>(configuration.GetSection("Database"));

            // Connection adapter com pool
            services.AddScoped<ISQLConnectionAdapter, SQLConnectionAdapter>();

            // Repositórios
            services.AddScoped<ISampleTaskRepository, SQLSampleTaskRepository>();

            return services;
        }
#endif

#if MongoDbCondition
        private static IServiceCollection AddMongoDbAdapters(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Configuração de settings
            services.Configure<DBSettings>(configuration.GetSection("Database"));

            // Connection adapter
            services.AddScoped<INoSQLConnectionAdapter, NoSQLConnectionAdapter>();

            // Repositórios
            services.AddScoped<ISampleTaskRepository, NoSQLSampleTaskRepository>();

            return services;
        }
#endif
    }
}
