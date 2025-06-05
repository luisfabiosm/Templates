using Azure;
using Domain.Core.Base;
using Domain.Core.Exceptions;
using Domain.Core.Models.Entity;
using Domain.Core.Models.Responses;
using System.ComponentModel.DataAnnotations;

namespace Domain.UseCases.Sample.AddSampleTask
{


    public class UseCaseAddSampleTask : BaseUseCaseHandler<TransactionAddSampleTask, BaseReturn<ResponseNewSampleTask>, SampleTask>
    {

        public UseCaseAddSampleTask(IServiceProvider serviceProvider) :base(serviceProvider)
        {
                
        }

        protected override async Task ValidateTransaction(TransactionAddSampleTask transaction, CancellationToken cancellationToken)
        {
            if (transaction.TimerInMilliseconds < 500)  
               _validateException.AddDetails(new ErrorDetails("O TIMER deve ser no minimo de 500 millisegundos", "TimerInMilliseconds"));

            if (_validateException.ErrorDetails.Count > 0)
                throw _validateException;
            

        }


        protected override async Task<SampleTask> ExecuteTransactionProcessing(TransactionAddSampleTask transaction, CancellationToken cancellationToken)
        {
            try
            {
                var _sampleDto = transaction.getSampleTaskDto();
               
#if SqlServerCondition || PSQLCondition
                var _result = await _sampleRepoSql.AddSampleTaskAsync(transaction);
#elif MongoDbCondition
                var _result = await _sampleRepoNoSql.AddSampleTaskAsync(transaction);
#endif

               var _handleResult = await HandleProcessingResult<SampleTask>(_result.Item1, _result.exception);

                return _handleResult;
            }
            catch (Exception dbEx)
            {
                _loggingAdapter.LogError("Database error", dbEx);
                throw;
            }
        }


        protected override BaseReturn<ResponseNewSampleTask> ReturnSuccessResponse(SampleTask result, string message, string correlationId)
        {
            return BaseReturn<ResponseNewSampleTask>.FromSuccess(
                new ResponseNewSampleTask(result),
                message,
                correlationId
            );
        }


        protected override BaseReturn<ResponseNewSampleTask> ReturnErrorResponse(Exception exception, string correlationId)
        {
            return new BaseReturn<ResponseNewSampleTask>(exception, false, correlationId);
        }


    }
}
