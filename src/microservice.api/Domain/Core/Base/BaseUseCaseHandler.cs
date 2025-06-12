using Domain.Core.Common.Mediator;
using Domain.Core.Common.Transactions;
using Domain.Core.Exceptions;
using Domain.Core.Models.Transactions;
using Domain.Core.Ports.Outbound;
using Domain.Services;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Domain.Core.Base
{
    public abstract class BaseUseCaseHandler<TTransaction, TResponse, TResult> : BaseService, IBSRequestHandler<TTransaction, TResponse>
        where TTransaction : BaseTransaction<TResponse>
        where TResponse : class
    {
        protected ValidateException? _validateException;
        protected readonly ActivitySource _activitySource;
        protected readonly ValidatorService _validateService;

#if SqlServerCondition || PSQLCondition
        protected readonly ISQLSampleRepository _sampleRepoSql;
#elif MongoDbCondition
        protected readonly INoSQLSampleRepository _sampleRepoNoSql;
#endif

        protected BaseUseCaseHandler(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _activitySource = serviceProvider.GetRequiredService<ActivitySource>();
            _validateService = serviceProvider.GetRequiredService<ValidatorService>();

#if SqlServerCondition || PSQLCondition
            _sampleRepoSql = serviceProvider.GetRequiredService<ISQLSampleRepository>();
#elif MongoDbCondition
            _sampleRepoNoSql = serviceProvider.GetRequiredService<INoSQLSampleRepository>();
#endif
        }


        public async ValueTask<TResponse> Handle(TTransaction transaction, CancellationToken cancellationToken)
        {

            var stopwatch = Stopwatch.StartNew();
            var operationName = $"{GetType().Name}.Handle";
            var correlationId = transaction.CorrelationId;

            using var operationContext = StartOperation(operationName, correlationId);

            try
            {
                AddTraceProperty("TransactionType", typeof(TTransaction).Name);
                AddTraceProperty("ResponseType", typeof(TResponse).Name);
                AddTraceProperty("HandlerType", GetType().Name);

                LogInformation("Iniciando processamento: {RequestType} [CorrelationId: {CorrelationId}]",
                  typeof(TTransaction).Name, correlationId);

                RecordRequest(operationName);

                // Pré-processamento (validações, etc)
                await PreProcessing(transaction, cancellationToken);


                // Processamento principal
                var _result = await Processing(transaction, cancellationToken);


                // Pós-processamento (logs, cache, eventos, etc)
                await PosProcessing(transaction, _result, cancellationToken);

                stopwatch.Stop();
                RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, operationName);

                LogInformation("Processamento concluído com sucesso: {RequestType} [CorrelationId: {CorrelationId}] em {Duration}ms",
                             typeof(TTransaction).Name, correlationId, stopwatch.ElapsedMilliseconds);

                return _result;
            }
            catch (BusinessException bex)
            {
                stopwatch.Stop();
                AddTraceProperty("ErrorType", "BusinessError");
                RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, $"{operationName}_BusinessError");
                return await HandleError("Handle", transaction, bex, cancellationToken);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddTraceProperty("ErrorType", "UnexpectedError");
                RecordRequestDuration(stopwatch.Elapsed.TotalSeconds, $"{operationName}_UnexpectedError");
                return await HandleError("Handle", transaction, ex, cancellationToken);
            }
        }


        protected virtual async ValueTask PreProcessing(TTransaction transaction, CancellationToken cancellationToken)
        {
            using var operationContext = StartOperation("PreProcessing", transaction.CorrelationId);

            AddTraceProperty("Phase", "PreProcessing");
            LogDebug("Iniciando PreProcessing para {TransactionType}", typeof(TTransaction).Name);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await ValidateBeforeProcess(transaction, cancellationToken);

                stopwatch.Stop();
                AddTraceProperty("PreProcessingDuration", stopwatch.ElapsedMilliseconds.ToString());
                LogDebug("PreProcessing concluído em {Duration}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddTraceProperty("PreProcessingError", ex.Message);
                LogError("Erro no PreProcessing após {Duration}ms: {Error}", ex, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }


        protected async ValueTask<TResponse> Processing(TTransaction transaction, CancellationToken cancellationToken)
        {
            var operationName = $"{GetType().Name}.Processing";
            using var operationContext = StartOperation(operationName, transaction.CorrelationId);

            AddTraceProperty("Phase", "Processing");
            LogInformation("Iniciando processamento principal: {HandlerType}", GetType().Name);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Executa o processamento específico da transação (lógica de negócio)
                var result = await ExecuteTransactionProcessing(transaction, cancellationToken);

                stopwatch.Stop();
                AddTraceProperty("ProcessingDuration", stopwatch.ElapsedMilliseconds.ToString());
                AddTraceProperty("ProcessingSuccess", "true");

                LogInformation("Processamento principal concluído com sucesso em {Duration}ms", stopwatch.ElapsedMilliseconds);

                // Retorna response com sucesso
                return ReturnSuccessResponse(result, "Transação executada com sucesso", transaction.CorrelationId);
            }
            catch (BusinessException bex)
            {
                stopwatch.Stop();
                AddTraceProperty("ProcessingDuration", stopwatch.ElapsedMilliseconds.ToString());
                AddTraceProperty("ProcessingSuccess", "false");
                AddTraceProperty("BusinessErrorCode", bex.ErrorCode.ToString());

                LogWarning("Business exception no processamento após {Duration}ms: {Error}",
                    stopwatch.ElapsedMilliseconds, bex.Message);
                throw;
            }
            catch (ValidateException vex)
            {
                stopwatch.Stop();
                AddTraceProperty("ProcessingDuration", stopwatch.ElapsedMilliseconds.ToString());
                AddTraceProperty("ProcessingSuccess", "false");
                AddTraceProperty("ValidationErrors", string.Join(", ", vex.ErrorDetails));

                LogWarning("Validation exception no processamento após {Duration}ms: {Errors}",
                    stopwatch.ElapsedMilliseconds, string.Join(", ", vex.ErrorDetails));
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddTraceProperty("ProcessingDuration", stopwatch.ElapsedMilliseconds.ToString());
                AddTraceProperty("ProcessingSuccess", "false");
                AddTraceProperty("UnexpectedError", ex.Message);

                LogError("Erro inesperado no processamento após {Duration}ms: {Error}",
                    ex, stopwatch.ElapsedMilliseconds, ex.Message);

                throw new InternalException("Erro interno ao processar o request", 500, ex);
            }
        }


        protected virtual async ValueTask PosProcessing(TTransaction transaction, TResponse response, CancellationToken cancellationToken)
        {
            using var operationContext = StartOperation("PosProcessing", transaction.CorrelationId);

            AddTraceProperty("Phase", "PosProcessing");
            LogDebug("Iniciando PosProcessing para {TransactionType}", typeof(TTransaction).Name);

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Se for um BaseReturn, adiciona informações de performance
                if (response is BaseReturn<TResponse> baseReturn && baseReturn.Success)
                {
                    AddTraceProperty("ResponseSuccess", "true");
                    AddTraceProperty("ResponseMessage", baseReturn.Message ?? "");

                    // Exemplo de pós-processamento: eventos, cache, notificações
                    await ProcessSuccessEvents(transaction, response, cancellationToken);
                }

                stopwatch.Stop();
                AddTraceProperty("PosProcessingDuration", stopwatch.ElapsedMilliseconds.ToString());
                LogDebug("PosProcessing concluído em {Duration}ms", stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AddTraceProperty("PosProcessingError", ex.Message);
                LogWarning("Erro no PosProcessing após {Duration}ms (não crítico): {Error}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                // PosProcessing não deve quebrar o fluxo principal
                // Log apenas o erro e continua
            }
        }


        protected virtual async ValueTask<TResponse> HandleError(string methodName, TTransaction transaction, Exception exception, CancellationToken cancellationToken)
        {
            using var operationContext = StartOperation($"HandleError.{methodName}", transaction.CorrelationId);

            AddTraceProperty("ErrorHandling", "true");
            AddTraceProperty("OriginalMethod", methodName);
            AddTraceProperty("ExceptionType", exception.GetType().Name);

            // Usa o método do BaseService para tratamento padronizado
            var handledException = HandleException(methodName, exception);

            // Log específico do handler
            LogError("Erro tratado em {HandlerType}.{MethodName}: {ErrorMessage}",
                handledException, GetType().Name, methodName, handledException.Message);

            // Retorna response de erro tipificado
            return await ReturnErrorResponse(handledException, transaction.CorrelationId, cancellationToken);
        }


        protected abstract ValueTask<TResult> ExecuteTransactionProcessing(TTransaction transaction, CancellationToken cancellationToken);
        protected abstract TResponse ReturnSuccessResponse(TResult result, string message, string correlationId);
        protected abstract ValueTask<TResponse> ReturnErrorResponse(Exception exception, string correlationId, CancellationToken cancellationToken);


        protected virtual async ValueTask ValidateBeforeProcess(TTransaction transaction, CancellationToken cancellationToken)
        {
            // Implementação padrão de validação
            if (_validateService != null)
            {
                LogDebug("Executando validações usando ValidatorService");
                // Aqui entraria a lógica de validação usando o _validateService
                await Task.CompletedTask;
            }
        }

        protected virtual async ValueTask ProcessSuccessEvents(TTransaction transaction, TResponse response, CancellationToken cancellationToken)
        {
            // Implementação padrão vazia - override conforme necessário
            LogDebug("ProcessSuccessEvents executado (implementação padrão vazia)");
            await Task.CompletedTask;
        }


    }

}
