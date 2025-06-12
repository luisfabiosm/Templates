using Adapters.Outbound.Database.NoSQL.Sample;
using Domain.Core.Ports.Outbound;
using Domain.Core.Settings;

namespace Adapters.Outbound.Database.NoSQL
{
    public static class NOSQLAdapterExtensions
    {
        public static IServiceCollection AddNoSQLAdapter(this IServiceCollection services, IConfiguration configuration)
        {
            #region NoSQL MongoDB Session Management

            services.Configure<DBSettings>(options =>
            {
                var dbSection = configuration.GetSection("AppSettings:DB");

                // Configurações específicas do MongoDB
                options.ServerUrl = GetEnvironmentVariableOrDefault("MONGODB_SERVER", dbSection.GetValue<string>("ServerUrl")) ?? string.Empty;
                options.Username = GetEnvironmentVariableOrDefault("MONGODB_USER", dbSection.GetValue<string>("Username")) ?? string.Empty;
                options.Password = GetEnvironmentVariableOrDefault("MONGODB_PASSWORD", dbSection.GetValue<string>("Password")) ?? string.Empty;
                options.Database = GetEnvironmentVariableOrDefault("MONGODB_DATABASE", dbSection.GetValue<string>("Database")) ?? string.Empty;
                options.CommandTimeout = dbSection.GetValue<int>("CommandTimeout", 30);
                options.ConnectTimeout = dbSection.GetValue<int>("ConnectTimeout", 30);
                options.Port = dbSection.GetValue<int>("Port", 27017);

                // Validações
                ValidateNoSQLConfiguration(options);
            });

            // Registrar serviços NoSQL
            services.AddSingleton<INoSQLConnectionAdapter, NoSQLConnectionAdapter>();
            services.AddScoped<INoSQLSampleRepository, NoSQLSampleRepository>();

            return services;

            #endregion
        }

        private static string? GetEnvironmentVariableOrDefault(string environmentVariable, string? defaultValue)
        {
            return Environment.GetEnvironmentVariable(environmentVariable) ?? defaultValue;
        }

        private static void ValidateNoSQLConfiguration(DBSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.ServerUrl))
            {
                throw new InvalidOperationException("MongoDB ServerUrl não pode ser nulo ou vazio");
            }

            if (string.IsNullOrWhiteSpace(settings.Database))
            {
                throw new InvalidOperationException("MongoDB Database não pode ser nulo ou vazio");
            }

            if (settings.CommandTimeout <= 0)
            {
                throw new InvalidOperationException("MongoDB CommandTimeout deve ser maior que zero");
            }

            if (settings.ConnectTimeout <= 0)
            {
                throw new InvalidOperationException("MongoDB ConnectTimeout deve ser maior que zero");
            }

            if (settings.Port <= 0)
            {
                throw new InvalidOperationException("MongoDB Port deve ser maior que zero");
            }
        }
    }
}
