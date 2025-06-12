using Adapters.Inbound.WebApi.Sample.Models;
using Domain.Core.Base;
using Domain.Core.Exceptions;
using Domain.Core.Models.Responses;
using Domain.UseCases.Sample.AddSampleTask;

using System.Linq.Expressions;

namespace Adapters.Inbound.WebApi.Sample.Mapping
{
    public class MappingHttpRequestToTransaction
    {
        public TransactionAddSampleTask ToTransactionAddSampleTask(NewSampleTaskRequest request)
        {
            try
            {
                return new TransactionAddSampleTask(request.TaskName, request.TimerInMilliseconds, true);
            }
            catch (ValidateException ex)
            {

                throw new ValidateException("Erro na validação do request",-1,ex.ErrorDetails);
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
}

      