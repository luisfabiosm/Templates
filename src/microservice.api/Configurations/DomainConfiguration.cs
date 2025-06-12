

using Domain.Core.Base;
using Domain.Core.Common.Mediator;
using Domain.Core.Common.ResultPattern;
using Domain.Core.Models.Responses;
using Domain.Core.Ports.Domain;
using Domain.Services;
using Domain.UseCases.Sample.AddSampleTask;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Configurations 
{
    public static class DomainConfiguration
    {

        public static IServiceCollection ConfigureDomainAdapters(this IServiceCollection services, IConfiguration configuration)
        {


            #region Domain MediatoR

            services.AddTransient<BSMediator>();
            services.AddTransient<IBSRequestHandler<TransactionAddSampleTask, BaseReturn<ResponseNewSampleTask>>, UseCaseAddSampleTaskHandler>(); //PARA CADA USECASE HANDLER

            #endregion


            #region Domain Services

            services.AddScoped<ISampleService, SampleService>();
            services.AddTransient<ValidatorService>();

            #endregion



            return services;
        }
    }
}
