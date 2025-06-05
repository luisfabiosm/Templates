using Adapters.Outbound.Database.SQL.Sample;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Models.Entity;
using Domain.Core.Settings;

namespace Adapters.Outbound.Database.SQL
{
    public static class SQLAdapterExtensions
    {
        public static IServiceCollection AddSQLAdapter(this IServiceCollection services, IConfiguration configuration)
        {

            #region SQL SERVER or Postgresql Session Management


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


            services.AddScoped<ISQLConnectionAdapter, SQLConnectionAdapter>();
            services.AddScoped<ISQLSampleRepository, SQLSampleRepository>();

            return services;

            #endregion
        }
    }
}
