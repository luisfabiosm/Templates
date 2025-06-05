using Domain.Core.Base;
using Domain.Core.Exceptions;
using Domain.Core.Models.Entity;
using Domain.Core.Models.Responses;
using Domain.UseCases.Sample.AddSampleTask;

namespace Domain.UseCases.Sample.GetSampleTask
{


    public class UseCaseGetSampleTask : BaseUseCaseHandler<TransactionGetSampleTask, BaseReturn<ResponseGetSampleTask>, SampleTask>
    {

        public UseCaseGetSampleTask(IServiceProvider serviceProvider) :base(serviceProvider)
        {
                
        }
        protected override async Task ValidateTransaction(TransactionGetSampleTask transaction, CancellationToken cancellationToken)
        {

            if (_validateException.ErrorDetails.Count > 0)
                throw _validateException;
            
        }

        protected override async Task<SampleTask> ExecuteTransactionProcessing(TransactionGetSampleTask transaction, CancellationToken cancellationToken)
        {
            try
            {
              

#if SqlServerCondition || PSQLCondition
                var _result = await _sampleRepoSql.GetSampleTaskByIdAsync(transaction);
#elif MongoDbCondition
                var _result = await _sampleRepoNoSql.GetSampleTaskByIdAsync(transaction);
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

        protected override BaseReturn<ResponseGetSampleTask> ReturnSuccessResponse(SampleTask result, string message, string correlationId)
        {
            return BaseReturn<ResponseGetSampleTask>.FromSuccess(
                new ResponseGetSampleTask(result),
                message,
                correlationId
            );
        }

        protected override BaseReturn<ResponseGetSampleTask> ReturnErrorResponse(Exception exception, string correlationId)
        {
            return new BaseReturn<ResponseGetSampleTask>(exception, false, correlationId);
        }

    }
}
