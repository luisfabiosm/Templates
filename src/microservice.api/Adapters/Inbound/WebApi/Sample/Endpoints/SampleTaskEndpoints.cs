using Adapters.Inbound.WebApi.Sample.Mapping;
using Adapters.Inbound.WebApi.Sample.Models;
using Azure.Core;
using Domain.Core.Base;
using Domain.Core.Common.Mediator;
using Domain.Core.Common.ResultPattern;
using Domain.Core.Models.Entity;
using Domain.Core.Models.Responses;
using Domain.UseCases.Sample.AddSampleTask;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Threading.Channels;

namespace Adapters.Inbound.WebApi.Sample.Endpoints
{

    //public static class ProcessadorSPARoute
    public static partial class SampleTaskEndpoints
    {
        public static void AddSampleTaskEndpoints(this WebApplication app)
        {
            app.MapPost("sample/v1/task", ProcPostRequest)
             .WithTags("Add New Sample Task")
             .WithName("AddSampleTask")
             .Accepts<NewSampleTaskRequest>("application/json")
             .Produces<ResponseNewSampleTask>(StatusCodes.Status200OK)
             .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
             .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);




        }



        #region POST

        private static async Task<IResult> ProcPostRequest(
               [FromBody] NewSampleTaskRequest request,
               [FromServices] BSMediator bSMediator,
               [FromServices] MappingHttpRequestToTransaction mapping,
               HttpContext context)
        {
            string correlationId = Guid.NewGuid().ToString();
            var transaction = mapping.ToTransactionAddSampleTask(request);

            if (transaction.GetType().GetProperty("CorrelationId") != null)
                transaction.GetType().GetProperty("CorrelationId").SetValue(transaction, correlationId);


            var response = await bSMediator.Send<TransactionAddSampleTask, BaseReturn<ResponseNewSampleTask>>(transaction);

            if (!response.Success)
                response.ThrowIfError();


            return Results.Ok(response.Data);

        }


        #endregion



    }
}
