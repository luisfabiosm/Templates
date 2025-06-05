using Adapters.Inbound.WebApi.Sample.Mapping;
using Adapters.Inbound.WebApi.Sample.Models;
using Azure.Core;
using Domain.Core.Base;
using Domain.Core.Interfaces.Domain;
using Domain.Core.Mediator;
using Domain.Core.Models.Entity;
using Domain.Core.Models.Responses;
using Domain.UseCases.Sample.AddSampleTask;
using Domain.UseCases.Sample.GetSampleTask;
using Domain.UseCases.Sample.ListSampleTask;
using Domain.UseCases.Sample.UpdateSampleTaskTimer;
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


            app.MapPut("sample/v1/task/timer", ProcPutTaskTimerRequest)
              .WithTags("Update Task Timer")
              .WithName("UpdateSampleTaskTimer")
              .Accepts<UpdateTaskTimerRequest>("application/json")
              .Produces<bool>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            app.MapGet("sample/v1/task/{id:int}", ProcGetByIdRequest)
              .WithTags("Get Sample Task by Id")
              .WithName("GetById")
              .Produces<ResponseGetSampleTask>(StatusCodes.Status200OK)
              .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
              .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);


            app.MapGet("sample/v1/task", ProcGetAllRequest)
              .WithTags("List all Sample Tasks")
              .WithName("GetAll")
              .Produces<ResponseListSampleTask>(StatusCodes.Status200OK)
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



        #region PUT

        private static async Task<IResult> ProcPutTaskTimerRequest(
             [FromBody] UpdateTaskTimerRequest request,
             [FromServices] BSMediator bSMediator,
             [FromServices] MappingHttpRequestToTransaction mapping,
             HttpContext context)
        {
            string correlationId = Guid.NewGuid().ToString();
            var transaction = mapping.ToTransactionUpdateSampleTaskTimer(request);

            if (transaction.GetType().GetProperty("CorrelationId") != null)
                transaction.GetType().GetProperty("CorrelationId").SetValue(transaction, correlationId);

            var response = await bSMediator.Send<TransactionUpdateSampleTaskTimer, BaseReturn<bool>>(transaction);

            if (!response.Success)
                response.ThrowIfError();

            return Results.Ok(response.Data);

        }


        #endregion



        #region GET

        private static async Task<IResult> ProcGetByIdRequest(
             int id,
             [FromServices] BSMediator bSMediator,
             [FromServices] MappingHttpRequestToTransaction mapping,
             HttpContext context)
        {
            string correlationId = Guid.NewGuid().ToString();
            var transaction = mapping.ToTransactionGetSampleTask(id);

            if (transaction.GetType().GetProperty("CorrelationId") != null)
                transaction.GetType().GetProperty("CorrelationId").SetValue(transaction, correlationId);

            var response = await bSMediator.Send<TransactionGetSampleTask, BaseReturn<ResponseGetSampleTask>>(transaction);

            if (!response.Success)
                response.ThrowIfError();

            return Results.Ok(response.Data);
        }


        private static async Task<IResult> ProcGetAllRequest(
            [FromServices] BSMediator bSMediator,
            [FromServices] MappingHttpRequestToTransaction mapping,
            HttpContext context)
        {
            string correlationId = Guid.NewGuid().ToString();

            var transaction = mapping.ToTransactionListSampleTask();

            if (transaction.GetType().GetProperty("CorrelationId") != null)
                transaction.GetType().GetProperty("CorrelationId").SetValue(transaction, correlationId);

            var response = await bSMediator.Send<TransactionListSampleTask, BaseReturn<ResponseListSampleTask>>(transaction);

            if (!response.Success)
                response.ThrowIfError();

            return Results.Ok(response.Data);
        }

        #endregion
    }
}
