using Adapters.Outbound.Database.NoSQL.Sample;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Settings;

namespace Adapters.Outbound.Database.NoSQL
{
    public static class NOSQLAdapterExtensions
    {

        public static IServiceCollection AddNoSQLAdapter(this IServiceCollection services, IConfiguration configuration)
        {

            #region Mongo Session Management


            services.Configure<DBSettings>(options =>
            {
                var _settings = configuration.GetSection("AppSettings:DB");

                options.ServerUrl = Environment.GetEnvironmentVariable("CLUSTER_SERVER") ?? _settings.GetValue<string>("ServerUrl");
                options.Username = Environment.GetEnvironmentVariable("USER") ?? _settings.GetValue<string>("Username");
                options.Password = Environment.GetEnvironmentVariable("CRIPT_PASSWORD") ?? _settings.GetValue<string>("Password");
                options.Database = Environment.GetEnvironmentVariable("DATABASE") ?? _settings.GetValue<string>("Database");
                options.CommandTimeout = _settings.GetValue<int>("CommandTimeout");
                options.ConnectTimeout = _settings.GetValue<int>("ConnectTimeout");
            });


            services.AddScoped<INoSQLConnectionAdapter, NoSQLConnectionAdapter>();
            services.AddScoped<INoSQLSampleRepository, NoSQLSampleRepository>();

            return services;

            #endregion
        }
    }
}
