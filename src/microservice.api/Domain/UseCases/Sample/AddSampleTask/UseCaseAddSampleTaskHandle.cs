using Domain.Core.Base;
using Domain.Core.Common.ResultPattern;
using Domain.Core.Exceptions;
using Domain.Core.Models.Dto;
using Domain.Core.Models.Entity;
using Domain.Core.Models.Entity.Domain.Core.Models.Entity;
using Domain.Core.Models.Responses;

namespace Domain.UseCases.Sample.AddSampleTask
{
    public class UseCaseAddSampleTaskHandler : BaseUseCaseHandler<TransactionAddSampleTask, BaseReturn<ResponseNewSampleTask>, SampleTask>
    {
        // Cache dos operationNames para evitar alocações desnecessárias
        private static readonly string ExecuteOperationName = "ExecuteTransactionProcessing.UseCaseAddSampleTaskHandler";
        private static readonly string SuccessOperationName = "ReturnSuccessResponse";
        private static readonly string ErrorOperationName = "ReturnErrorResponse";
        private static readonly string ValidationOperationName = "ValidateBeforeProcess.AddSampleTask";
        private static readonly string EventsOperationName = "ProcessSuccessEvents.AddSampleTask";

        // Cache das mensagens mais comuns
        private static readonly string TaskNameRequiredMessage = "Nome da task é obrigatório";
        private static readonly string TimerInvalidMessage = "Timer deve ser maior que zero";
        private static readonly string TransactionSuccessMessage = "Task criada com sucesso";

        public UseCaseAddSampleTaskHandler(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override async ValueTask<SampleTask> ExecuteTransactionProcessing(
            TransactionAddSampleTask transaction,
            CancellationToken cancellationToken)
        {
            using var operationContext = StartOperation(ExecuteOperationName, transaction.CorrelationId);

            var sampleDto = transaction.getSampleTaskDto();
            SetTracePropertiesForExecution(sampleDto);

            LogInformation("Executando criação de nova task: {TaskName} com timer {Timer}ms",
                sampleDto.Name, sampleDto.TimerOnMilliseconds);

            try
            {
                var result = await PersistSampleTask(transaction);
                RecordSuccessMetrics(result);
                return result;
            }
            catch (Exception ex)
            {
                RecordErrorMetrics(sampleDto, ex);
                throw;
            }
        }

        protected override BaseReturn<ResponseNewSampleTask> ReturnSuccessResponse(
            SampleTask result,
            string message,
            string correlationId)
        {
            using var operationContext = StartOperation(SuccessOperationName, correlationId);

            SetTracePropertiesForSuccess(result);
            LogDebug("Criando response de sucesso para task: {TaskId}", result.Id);

            return BaseReturn<ResponseNewSampleTask>.FromSuccess(
                new ResponseNewSampleTask
                {
                    Id = result.Id,
                    Name = result.Name,
                    TimerOnMiliseconds = result.TimerOnMiliseconds
                },
                message,
                correlationId);
        }

        protected override async ValueTask<BaseReturn<ResponseNewSampleTask>> ReturnErrorResponse(
            Exception exception,
            string correlationId,
            CancellationToken cancellationToken)
        {
            using var operationContext = StartOperation(ErrorOperationName, correlationId);

            SetTracePropertiesForError(exception);
            LogError("Criando response de erro: {ErrorMessage}", exception, exception.Message);

            var errorResponse = new BaseReturn<ResponseNewSampleTask>(exception, false, correlationId);

            return await ValueTask.FromResult(errorResponse);
        }

        protected override async ValueTask ValidateBeforeProcess(
            TransactionAddSampleTask transaction,
            CancellationToken cancellationToken)
        {
            using var operationContext = StartOperation(ValidationOperationName, transaction.CorrelationId);

            LogDebug("Executando validações específicas para AddSampleTask");

            var sampleTaskDto = transaction.getSampleTaskDto();
            ValidateTaskData(sampleTaskDto);

            AddTraceProperty("ValidationResult", "Success");
            LogDebug("Validações específicas concluídas com sucesso");

            await base.ValidateBeforeProcess(transaction, cancellationToken);
        }

        protected override async ValueTask ProcessSuccessEvents(
            TransactionAddSampleTask transaction,
            BaseReturn<ResponseNewSampleTask> response,
            CancellationToken cancellationToken)
        {
            using var operationContext = StartOperation(EventsOperationName, transaction.CorrelationId);

            LogDebug("Processando eventos de sucesso para task criada: {TaskId}", response.Data?.Id);

            // Implementações futuras de eventos de domínio, cache, notificações, etc.
            AddTraceProperty("EventsProcessed", "TaskCreated");
            LogDebug("Eventos de sucesso processados com sucesso");

            await base.ProcessSuccessEvents(transaction, response, cancellationToken);
        }

        // Métodos privados para reduzir complexidade e melhorar performance

        private async ValueTask<SampleTask> PersistSampleTask(TransactionAddSampleTask transaction)
        {
#if SqlServerCondition || PSQLCondition
            LogDebug("Usando repositório SQL para persistência");
            AddTraceProperty("RepositoryType", "SQL");
            return await _sampleRepoSql.AddSampleTaskAsync(transaction);
#elif MongoDbCondition
            LogDebug("Usando repositório NoSQL para persistência");
            AddTraceProperty("RepositoryType", "NoSQL");
            return await _sampleRepoNoSql.AddSampleTaskAsync(transaction);
#endif
        }

        private static void ValidateTaskData(SampleTaskDto sampleTaskDto)
        {
            if (string.IsNullOrWhiteSpace(sampleTaskDto.Name))
            {
                throw new ValidateException(TaskNameRequiredMessage);
            }

            if (sampleTaskDto.TimerOnMilliseconds <= 0)
            {
                throw new ValidateException(TimerInvalidMessage);
            }
        }

        private void SetTracePropertiesForExecution(SampleTaskDto sampleDto)
        {
            AddTraceProperty("TaskName", sampleDto.Name);
            AddTraceProperty("TaskTimer", sampleDto.TimerOnMilliseconds.ToString());
        }

        private void SetTracePropertiesForSuccess(SampleTask result)
        {
            AddTraceProperty("ResponseType", "Success");
            AddTraceProperty("ResponseEntityId", result.Id.ToString());
        }

        private void SetTracePropertiesForError(Exception exception)
        {
            AddTraceProperty("ResponseType", "Error");
            AddTraceProperty("ErrorType", exception.GetType().Name);
        }

        private void RecordSuccessMetrics(SampleTask result)
        {
            AddTraceProperty("CreatedEntityId", result.Id.ToString());
            AddTraceProperty("ExecutionResult", "Success");

            LogInformation("Task criada com sucesso: {TaskId} - {TaskName}", result.Id, result.Name);
            RecordRequest("AddSampleTask.Success");
        }

        private void RecordErrorMetrics(SampleTaskDto sampleDto, Exception ex)
        {
            AddTraceProperty("ExecutionResult", "Error");
            AddTraceProperty("ErrorDetails", ex.Message);

            LogError("Erro ao criar task: {TaskName}. Erro: {Error}", ex, sampleDto.Name, ex.Message);
            RecordRequest("AddSampleTask.Error");
        }
    }
}