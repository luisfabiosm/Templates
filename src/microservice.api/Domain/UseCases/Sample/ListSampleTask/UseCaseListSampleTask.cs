using Domain.Core.Base;
using Domain.Core.Exceptions;
using Domain.Core.Models.Entity;
using Domain.Core.Models.Responses;
using Domain.UseCases.Sample.GetSampleTask;

namespace Domain.UseCases.Sample.ListSampleTask
{

    public class UseCaseListSampleTask : BaseUseCaseHandler<TransactionListSampleTask, BaseReturn<ResponseListSampleTask>, List<SampleTask>>
    {

        public UseCaseListSampleTask(IServiceProvider serviceProvider) :base(serviceProvider)
        {
                
        }

        protected override async Task ValidateTransaction(TransactionListSampleTask transaction, CancellationToken cancellationToken)
        {

            if (_validateException.ErrorDetails.Count > 0)
                throw _validateException;
            
        }

        protected override async Task<List<SampleTask>> ExecuteTransactionProcessing(TransactionListSampleTask transaction, CancellationToken cancellationToken)
        {

            try
            {
              
#if SqlServerCondition || PSQLCondition

               var _result = await _sampleRepoSql.ListAllSampleTaskAsync(transaction);
#elif MongoDbCondition

                var _result = await _sampleRepoNoSql.ListAllSampleTaskAsync(transaction);
#endif

                var _handleResult = await HandleProcessingResult<List<SampleTask>>(_result.Item1, _result.exception);

                return _handleResult;
            }
            catch (Exception dbEx)
            {
                _loggingAdapter.LogError("Database error", dbEx);
                throw;
            }
        }


        protected override BaseReturn<ResponseListSampleTask> ReturnSuccessResponse(List<SampleTask> result, string message, string correlationId)
        {
            return BaseReturn<ResponseListSampleTask>.FromSuccess(
                new ResponseListSampleTask(result),
                message,
                correlationId
            );
        }

        protected override BaseReturn<ResponseListSampleTask> ReturnErrorResponse(Exception exception, string correlationId)
        {
            return new BaseReturn<ResponseListSampleTask>(exception, false, correlationId);
        }

    }
}
