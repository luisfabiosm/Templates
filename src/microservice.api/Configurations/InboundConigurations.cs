using Adapters.Inbound.Extensions;

namespace Configurations
{
    public static class InboundConigurations
    {

        public static IServiceCollection ConfigureInboundAdapters(this IServiceCollection services, IConfiguration configuration)
        {

            services.addWebApiEndpoints(configuration);

            return services;
        }
      
    }
}