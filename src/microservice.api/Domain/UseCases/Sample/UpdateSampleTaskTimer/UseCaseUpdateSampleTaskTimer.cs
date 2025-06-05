using Domain.Core.Base;
using Domain.Core.Exceptions;
using Domain.Core.Models.Entity;
using System.ComponentModel.DataAnnotations;

namespace Domain.UseCases.Sample.UpdateSampleTaskTimer
{


    public class UseCaseUpdateSampleTaskTimer : BaseUseCaseHandler<TransactionUpdateSampleTaskTimer, BaseReturn<bool>, bool>
    {

        public UseCaseUpdateSampleTaskTimer(IServiceProvider serviceProvider) :base(serviceProvider)
        {
                
        }

        protected override async Task ValidateTransaction(TransactionUpdateSampleTaskTimer transaction, CancellationToken cancellationToken)
        {
            if (transaction.TimerInMilliseconds < 500)
                _validateException.AddDetails(new ErrorDetails("O TIMER deve ser no minimo de 500 millisegundos", "TimerInMilliseconds"));

            if (_validateException.ErrorDetails.Count > 0)
                throw _validateException;
        }



        protected override async Task<bool> ExecuteTransactionProcessing(TransactionUpdateSampleTaskTimer transaction, CancellationToken cancellationToken)
        {
            try
            {
               
#if SqlServerCondition || PSQLCondition

               var _result = await _sampleRepoSql.UpdateSampleTaskTimerAsync(transaction);
#elif MongoDbCondition

               var _result = await _sampleRepoNoSql.UpdateSampleTaskTimerAsync(transaction);
#endif

                var _handleResult = await HandleProcessingResult<bool>(_result.Item1, _result.exception);

                return _handleResult;
            }
            catch (Exception dbEx)
            {
                _loggingAdapter.LogError("Database error", dbEx);
                throw;
            }
        }

        protected override BaseReturn<bool> ReturnSuccessResponse(bool result, string message, string correlationId)
        {
            return BaseReturn<bool>.FromSuccess(
                result,
                message,
                correlationId
            );
        }


        protected override BaseReturn<bool> ReturnErrorResponse(Exception exception, string correlationId)
        {
            return new BaseReturn<bool>(exception, false, correlationId);
        }


    }
}
