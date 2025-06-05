using Domain.Core.Base;
using Domain.Core.Interfaces.Domain;
using Domain.Core.Mediator;
using Domain.Core.Models.Responses;
using Domain.Services;
using Domain.UseCases.Sample.AddSampleTask;
using Domain.UseCases.Sample.GetSampleTask;
using Domain.UseCases.Sample.ListSampleTask;
using Domain.UseCases.Sample.UpdateSampleTaskTimer;
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
            services.AddTransient<IBSRequestHandler<TransactionGetSampleTask, BaseReturn<ResponseGetSampleTask>>, UseCaseGetSampleTask>(); //PARA CADA USECASE HANDLER
            services.AddTransient<IBSRequestHandler<TransactionAddSampleTask, BaseReturn<ResponseNewSampleTask>>, UseCaseAddSampleTask>(); //PARA CADA USECASE HANDLER
            services.AddTransient<IBSRequestHandler<TransactionUpdateSampleTaskTimer, BaseReturn<bool>>, UseCaseUpdateSampleTaskTimer>(); //PARA CADA USECASE HANDLER
            services.AddTransient<IBSRequestHandler<TransactionListSampleTask, BaseReturn<ResponseListSampleTask>>, UseCaseListSampleTask>(); //PARA CADA USECASE HANDLER

            #endregion


            #region Domain Services

            services.AddScoped<ISampleService, SampleService>();
            services.AddTransient<ValidatorService>();

            #endregion



            return services;
        }
    }
}
