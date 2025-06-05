using Microsoft.Extensions.DependencyInjection;

namespace Configurations
{
    /// <summary>
    /// Configuração de adaptadores de mensageria seguindo OCP
    /// </summary>
    public static class MessagingConfiguration
    {
        public static IServiceCollection AddMessagingAdapters(
            this IServiceCollection services,
            IConfiguration configuration)
        {
#if KafkaCondition
            services.AddKafkaAdapters(configuration);
#endif

#if RabbitMQCondition
            services.AddRabbitMQAdapters(configuration);
#endif

            return services;
        }

#if KafkaCondition
        private static IServiceCollection AddKafkaAdapters(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Configuração Kafka
            services.Configure<KafkaSettings>(configuration.GetSection("Kafka"));
            services.AddSingleton<IKafkaProducer, KafkaProducer>();
            services.AddSingleton<IKafkaConsumer, KafkaConsumer>();

            return services;
        }
#endif

#if RabbitMQCondition
        private static IServiceCollection AddRabbitMQAdapters(
            this IServiceCollection services, 
            IConfiguration configuration)
        {
            // Configuração RabbitMQ
            services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
            services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
            services.AddSingleton<IRabbitMQConsumer, RabbitMQConsumer>();

            return services;
        }
#endif
    }
}
