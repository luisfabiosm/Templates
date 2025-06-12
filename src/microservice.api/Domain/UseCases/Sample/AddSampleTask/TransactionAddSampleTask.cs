using Domain.Core.Base;
using Domain.Core.Models.Dto;
using Domain.Core.Models.Responses;
using Domain.Core.Common.ResultPattern;
using Domain.Core.Common.Mediator;

namespace Domain.UseCases.Sample.AddSampleTask
{

    #region Transaction

    public readonly record struct TransactionAddSampleTask : IBSRequest<BaseReturn<ResponseNewSampleTask>>
    {
        // Composição - BaseTransaction dentro da struct
        private readonly BaseTransaction<BaseReturn<ResponseNewSampleTask>> _baseTransaction;

        // Propriedades específicas
        public string Name { get; }
        public int TimerInMilliseconds { get; }
        public bool IsEnable { get; }

        // Propriedades delegadas para BaseTransaction
        public int Code => _baseTransaction.Code;
        public string CorrelationId => _baseTransaction.CorrelationId;

        // Construtor principal
        public TransactionAddSampleTask(string name, int timer, bool isEnable)
        {
            Name = name;
            TimerInMilliseconds = timer;
            IsEnable = isEnable;
            _baseTransaction = BaseTransaction<BaseReturn<ResponseNewSampleTask>>.Create();
        }

        // Construtor com CorrelationId customizado
        public TransactionAddSampleTask(string name, int timer, bool isEnable, string correlationId)
        {
            Name = name;
            TimerInMilliseconds = timer;
            IsEnable = isEnable;
            _baseTransaction = BaseTransaction<BaseReturn<ResponseNewSampleTask>>.CreateWithCorrelationId(correlationId);
        }

        // Factory method com validação básica
        public static BaseReturn<TransactionAddSampleTask> Create(string name, int timer, bool isEnable)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BaseReturn<TransactionAddSampleTask>.FromError("Nome é obrigatório", 400);

            if (timer < 500)
                return BaseReturn<TransactionAddSampleTask>.FromError("Timer deve ser >= 500ms", 400);

            var transaction = new TransactionAddSampleTask(name, timer, isEnable);
            return BaseReturn<TransactionAddSampleTask>.FromSuccess(transaction, "Transaction criada");
        }

        // Factory com CorrelationId customizado
        public static BaseReturn<TransactionAddSampleTask> CreateWithCorrelationId(string name, int timer, bool isEnable, string correlationId)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BaseReturn<TransactionAddSampleTask>.FromError("Nome é obrigatório", 400);

            if (timer < 500)
                return BaseReturn<TransactionAddSampleTask>.FromError("Timer deve ser >= 500ms", 400);

            if (string.IsNullOrWhiteSpace(correlationId))
                return BaseReturn<TransactionAddSampleTask>.FromError("CorrelationId é obrigatório", 400);

            var transaction = new TransactionAddSampleTask(name, timer, isEnable, correlationId);
            return BaseReturn<TransactionAddSampleTask>.FromSuccess(transaction, "Transaction criada");
        }

        // Para compatibilidade com código existente
        public SampleTaskDto getSampleTaskDto() => new(Name, IsEnable, TimerInMilliseconds);

        // Clonagem mantendo BaseTransaction
        public TransactionAddSampleTask WithTimer(int newTimer)
        {
            var newBase = _baseTransaction.WithCorrelationId(CorrelationId); // Manter mesmo CorrelationId
            return new TransactionAddSampleTask(Name, newTimer, IsEnable, newBase.CorrelationId);
        }

        public TransactionAddSampleTask WithName(string newName)
        {
            var newBase = _baseTransaction.WithCorrelationId(CorrelationId);
            return new TransactionAddSampleTask(newName, TimerInMilliseconds, IsEnable, newBase.CorrelationId);
        }

        public TransactionAddSampleTask WithEnabled(bool enabled)
        {
            var newBase = _baseTransaction.WithCorrelationId(CorrelationId);
            return new TransactionAddSampleTask(Name, TimerInMilliseconds, enabled, newBase.CorrelationId);
        }

        public TransactionAddSampleTask WithCorrelationId(string newCorrelationId)
        {
            return new TransactionAddSampleTask(Name, TimerInMilliseconds, IsEnable, newCorrelationId);
        }

        public override string ToString()
            => $"TransactionAddSampleTask(Code: {Code}, CorrelationId: {CorrelationId[..Math.Min(8, CorrelationId.Length)]}, Name: {Name}, Timer: {TimerInMilliseconds}ms, Enabled: {IsEnable})";
    }


    #endregion

    #region Extensions

    public static class TransactionAddSampleTaskExtensions
    {

        public static async Task<BaseReturn<ResponseNewSampleTask>> ExecuteAsync<THandler>(
            this BaseReturn<TransactionAddSampleTask> transactionResult,
            THandler handler,
            CancellationToken cancellationToken = default)
        {
            if (!transactionResult.IsSuccess)
                return BaseReturn<ResponseNewSampleTask>.FromError(
                    transactionResult.Message,
                    transactionResult.ErrorCode,
                    transactionResult.CorrelationId);

            var transaction = transactionResult.Data;

            try
            {
                // Simular execução (substituir pela lógica real do handler)
                await Task.Delay(1, cancellationToken);

                // Criar response usando construtor simples
                var response = new ResponseNewSampleTask(
                    id: 1, // ID real viria do banco de dados
                    name: transaction.Name,
                    isTimer: transaction.IsEnable,
                    timerOnMiliseconds: transaction.TimerInMilliseconds
                );

                return BaseReturn<ResponseNewSampleTask>.FromSuccess(
                    response,
                    "Task criada com sucesso",
                    transaction.CorrelationId);
            }
            catch (Exception ex)
            {
                return BaseReturn<ResponseNewSampleTask>.FromException(ex, transaction.CorrelationId);
            }
        }


        public static BaseReturn<TransactionAddSampleTask> ValidateBusinessRules(
            this BaseReturn<TransactionAddSampleTask> transactionResult)
        {
            if (!transactionResult.IsSuccess)
                return transactionResult;

            var transaction = transactionResult.Data;

            // Regra 1: Tasks habilitadas precisam de timer >= 1000ms
            if (transaction.IsEnable && transaction.TimerInMilliseconds < 1000)
            {
                return BaseReturn<TransactionAddSampleTask>.FromError(
                    "Tasks habilitadas precisam de timer >= 1000ms",
                    400,
                    transaction.CorrelationId);
            }

            // Regra 2: Timer não pode exceder 1 hora
            if (transaction.TimerInMilliseconds > 3600000)
            {
                return BaseReturn<TransactionAddSampleTask>.FromError(
                    "Timer não pode exceder 1 hora",
                    400,
                    transaction.CorrelationId);
            }

            // Regra 3: Nome deve ter pelo menos 3 caracteres
            if (transaction.Name.Length < 3)
            {
                return BaseReturn<TransactionAddSampleTask>.FromError(
                    "Nome deve ter pelo menos 3 caracteres",
                    400,
                    transaction.CorrelationId);
            }

            return BaseReturn<TransactionAddSampleTask>.FromSuccess(
                transaction,
                "Validação de negócio bem-sucedida",
                transaction.CorrelationId);
        }


        public static BaseReturn<TransactionAddSampleTask> WithTimeout(
            this BaseReturn<TransactionAddSampleTask> transactionResult,
            TimeSpan timeout)
        {
            if (!transactionResult.IsSuccess)
                return transactionResult;

            var transaction = transactionResult.Data;

            // Para demonstração, sempre passa (implementação real verificaria idade da transaction)
            return BaseReturn<TransactionAddSampleTask>.FromSuccess(
                transaction,
                $"Timeout de {timeout.TotalMinutes} minutos aplicado",
                transaction.CorrelationId);
        }


        public static BaseReturn<TransactionAddSampleTask> LogTransaction(
            this BaseReturn<TransactionAddSampleTask> transactionResult,
            Action<string> logger)
        {
            if (transactionResult.IsSuccess)
            {
                var transaction = transactionResult.Data;
                logger($"Transaction válida: {transaction.Name} ({transaction.TimerInMilliseconds}ms)");
            }
            else
            {
                logger($"Transaction inválida: {transactionResult.Message}");
            }

            return transactionResult; // Pass-through para continuar pipeline
        }


        public static BaseReturn<TransactionAddSampleTask> Transform(
            this BaseReturn<TransactionAddSampleTask> transactionResult,
            Func<TransactionAddSampleTask, TransactionAddSampleTask> transformer)
        {
            if (!transactionResult.IsSuccess)
                return transactionResult;

            try
            {
                var transformedTransaction = transformer(transactionResult.Data);
                return BaseReturn<TransactionAddSampleTask>.FromSuccess(
                    transformedTransaction,
                    "Transaction transformada com sucesso",
                    transformedTransaction.CorrelationId);
            }
            catch (Exception ex)
            {
                return BaseReturn<TransactionAddSampleTask>.FromException(ex, transactionResult.CorrelationId);
            }
        }

        public static BaseReturn<TransactionAddSampleTask> ValidateIf(
            this BaseReturn<TransactionAddSampleTask> transactionResult,
            Func<TransactionAddSampleTask, bool> condition,
            Func<TransactionAddSampleTask, BaseReturn<TransactionAddSampleTask>> validator)
        {
            if (!transactionResult.IsSuccess)
                return transactionResult;

            var transaction = transactionResult.Data;

            if (condition(transaction))
            {
                return validator(transaction);
            }

            return BaseReturn<TransactionAddSampleTask>.FromSuccess(
                transaction,
                "Validação condicional ignorada",
                transaction.CorrelationId);
        }


        public static async Task<BaseReturn<ResponseNewSampleTask>> ExecuteWithRetryAsync<THandler>(
            this BaseReturn<TransactionAddSampleTask> transactionResult,
            THandler handler,
            int maxRetries = 3,
            TimeSpan delay = default,
            CancellationToken cancellationToken = default)
        {
            if (!transactionResult.IsSuccess)
                return BaseReturn<ResponseNewSampleTask>.FromError(
                    transactionResult.Message,
                    transactionResult.ErrorCode,
                    transactionResult.CorrelationId);

            if (delay == default)
                delay = TimeSpan.FromMilliseconds(100);

            var lastException = new Exception("No attempts made");

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var result = await transactionResult.ExecuteAsync(handler, cancellationToken);
                    if (result.IsSuccess)
                        return result;

                    lastException = new Exception(result.Message);
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }

                if (attempt < maxRetries)
                {
                    await Task.Delay(delay * attempt, cancellationToken); // Exponential backoff
                }
            }

            return BaseReturn<ResponseNewSampleTask>.FromException(
                new Exception($"Falha após {maxRetries} tentativas. Último erro: {lastException.Message}", lastException),
                transactionResult.CorrelationId);
        }
    }


    #endregion

}