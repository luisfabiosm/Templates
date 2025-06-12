using Adapters.Outbound.Database.SQL.Sample;
using Domain.Core.Models.Entity;
using Domain.Core.Ports.Outbound;
using Domain.Core.Settings;

namespace Adapters.Outbound.Database.SQL
{
    public static class SQLAdapterExtensions
    {
        public static IServiceCollection AddSQLAdapter(this IServiceCollection services, IConfiguration configuration)
        {
            #region SQL Server or PostgreSQL Session Management

            services.Configure<DBSettings>(options =>
            {
                var dbSection = configuration.GetSection("AppSettings:DB");

                // Configurações específicas do SQL/PostgreSQL
                options.ServerUrl = GetEnvironmentVariableOrDefault("SQL_SERVER", dbSection.GetValue<string>("ServerUrl")) ?? string.Empty;
                options.Username = GetEnvironmentVariableOrDefault("SQL_USER", dbSection.GetValue<string>("Username")) ?? string.Empty;
                options.Password = GetEnvironmentVariableOrDefault("SQL_PASSWORD", dbSection.GetValue<string>("Password")) ?? string.Empty;
                options.Database = GetEnvironmentVariableOrDefault("SQL_DATABASE", dbSection.GetValue<string>("Database")) ?? string.Empty;
                options.CommandTimeout = dbSection.GetValue<int>("CommandTimeout", 30);
                options.ConnectTimeout = dbSection.GetValue<int>("ConnectTimeout", 30);

#if PSQLCondition
                options.Port = dbSection.GetValue<int>("Port", 5432); // PostgreSQL default port
#elif SqlServerCondition
                options.Port = dbSection.GetValue<int>("Port", 1433); // SQL Server default port
#endif

                // Validações
                ValidateSQLConfiguration(options);
            });

            // Registrar serviços SQL
            services.AddScoped<ISQLConnectionAdapter, SQLConnectionAdapter>();
            services.AddScoped<ISQLSampleRepository, SQLSampleRepository>();

            return services;

            #endregion
        }
        private static string? GetEnvironmentVariableOrDefault(string environmentVariable, string? defaultValue)
        {
            return Environment.GetEnvironmentVariable(environmentVariable) ?? defaultValue;
        }

        private static void ValidateSQLConfiguration(DBSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ServerUrl))
            {
                throw new InvalidOperationException("SQL ServerUrl não pode ser nulo ou vazio");
            }

            if (string.IsNullOrWhiteSpace(settings.Database))
            {
                throw new InvalidOperationException("SQL Database não pode ser nulo ou vazio");
            }

            if (string.IsNullOrWhiteSpace(settings.Username))
            {
                throw new InvalidOperationException("SQL Username não pode ser nulo ou vazio");
            }

            if (string.IsNullOrWhiteSpace(settings.Password))
            {
                throw new InvalidOperationException("SQL Password não pode ser nulo ou vazio");
            }

            if (settings.CommandTimeout <= 0)
            {
                throw new InvalidOperationException("SQL CommandTimeout deve ser maior que zero");
            }

            if (settings.ConnectTimeout <= 0)
            {
                throw new InvalidOperationException("SQL ConnectTimeout deve ser maior que zero");
            }

            if (settings.Port <= 0)
            {
                throw new InvalidOperationException("SQL Port deve ser maior que zero");
            }
        }
    }
}
