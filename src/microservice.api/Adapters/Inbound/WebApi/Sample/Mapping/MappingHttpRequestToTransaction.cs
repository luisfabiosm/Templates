using Adapters.Inbound.WebApi.Sample.Models;
using Domain.Core.Base;
using Domain.Core.Exceptions;
using Domain.Core.Models.Responses;
using Domain.UseCases.Sample.AddSampleTask;
using Domain.UseCases.Sample.GetSampleTask;
using Domain.UseCases.Sample.ListSampleTask;
using Domain.UseCases.Sample.UpdateSampleTaskTimer;
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

        public TransactionUpdateSampleTaskTimer ToTransactionUpdateSampleTaskTimer(UpdateTaskTimerRequest request)
        {
            try
            {
                return new TransactionUpdateSampleTaskTimer(request.Id, request.TimerInMilliseconds);
            }
            catch (ValidateException ex)
            {

                throw new ValidateException("Erro na validação do request", -1, ex.ErrorDetails);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public TransactionGetSampleTask ToTransactionGetSampleTask(int id)
        {
            try
            {
                return new TransactionGetSampleTask(id);
            }
            catch (ValidateException ex)
            {

                throw new ValidateException("Erro na validação do request", -1, ex.ErrorDetails);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public TransactionListSampleTask ToTransactionListSampleTask()
        {
            return new TransactionListSampleTask();
        }
    }
}

      