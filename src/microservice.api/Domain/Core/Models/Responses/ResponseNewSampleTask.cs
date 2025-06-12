using Domain.Core.Common.ResultPattern;
using Domain.Core.Models.Entity;
using Domain.Core.Models.Entity.Domain.Core.Models.Entity;

namespace Domain.Core.Models.Responses
{

    #region Response

    public record ResponseNewSampleTask
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsTimer { get; set; }
        public int TimerOnMiliseconds { get; set; }

        // Construtor padrão necessário
        public ResponseNewSampleTask()
        {
        }

        // Construtor principal
        public ResponseNewSampleTask(int id, string name, bool isTimer, int timerOnMiliseconds)
        {
            Id = id;
            Name = name ?? string.Empty;
            IsTimer = isTimer;
            TimerOnMiliseconds = timerOnMiliseconds;
        }

        // Construtor a partir da entidade SampleTask
        public ResponseNewSampleTask(SampleTask sampleTask)
        {
            Id = sampleTask.Id;
            Name = sampleTask.Name ?? string.Empty;
            IsTimer = sampleTask.IsTimer;
            TimerOnMiliseconds = sampleTask.TimerOnMiliseconds;
        }

        // Factory method simples
        public static ResponseNewSampleTask FromEntity(SampleTask sampleTask)
            => new(sampleTask);

        // Validação básica
        public bool IsValid => Id > 0 && !string.IsNullOrWhiteSpace(Name);

        public override string ToString()
            => $"ResponseNewSampleTask(Id: {Id}, Name: {Name}, Timer: {TimerOnMiliseconds}ms, Active: {IsTimer})";
    }

    #endregion

    #region Extensions

    public static class ResponseNewSampleTaskExtensions
    {
        /// <summary>
        /// Valida se o response está correto
        /// </summary>
        public static BaseReturn<ResponseNewSampleTask> Validate(
            this BaseReturn<ResponseNewSampleTask> responseResult)
        {
            if (!responseResult.IsSuccess)
                return responseResult;

            var response = responseResult.Data;

            if (!response.IsValid)
            {
                return BaseReturn<ResponseNewSampleTask>.FromError(
                    "Response inválido: ID deve ser > 0 e Nome não pode ser vazio",
                    400,
                    responseResult.CorrelationId);
            }

            return BaseReturn<ResponseNewSampleTask>.FromSuccess(
                response,
                "Response validado com sucesso",
                responseResult.CorrelationId);
        }

        /// <summary>
        /// Log do resultado
        /// </summary>
        public static BaseReturn<ResponseNewSampleTask> LogResult(
            this BaseReturn<ResponseNewSampleTask> responseResult,
            Action<string> logger)
        {
            if (responseResult.IsSuccess)
            {
                var response = responseResult.Data;
                logger($"Task criada com sucesso: ID={response.Id}, Nome={response.Name}");
            }
            else
            {
                logger($"Falha na criação da task: {responseResult.Message}");
            }

            return responseResult; // Pass-through
        }
    }


    #endregion
}