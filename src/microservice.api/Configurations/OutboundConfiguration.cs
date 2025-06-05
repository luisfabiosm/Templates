
// Conditional using directives
#if SqlServerCondition || PSQLCondition
using Adapters.Outbound.Database.SQL;
#elif MongoDbCondition
using Adapters.Outbound.Database.NoSQL;
#endif

using Adapters.Outbound.Logging;
using Adapters.Outbound.Metrics;

namespace Configurations
{ 
    public static class OutboundConfiguration
    {
        public static IServiceCollection ConfigureOutboundAdapters(this IServiceCollection services, IConfiguration configuration)
        {

            #region Logging

            services.AddLoggingAdapter(configuration);

            #endregion region


            #region Metrics

            services.AddMetricsAdapter(configuration);

            #endregion


#if SqlServerCondition || PSQLCondition

            #region Database SQL or PostgreSQL

            services.AddSQLAdapter(configuration);

            #endregion

#elif MongoDbCondition


            #region Database NoSQL

            services.AddNoSQLAdapter(configuration);

            #endregion
#endif


            return services;
        }
    }
}
