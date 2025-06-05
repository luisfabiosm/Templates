using Domain.Core.Base;
using Domain.Core.ResultPattern;


namespace Domain.UseCases.Sample.AddSampleTask
{
    /// <summary>
    /// Validador para AddSampleTaskCommand seguindo SRP
    /// </summary>
    public sealed class AddSampleTaskValidator : IValidator<TransactionAddSampleTask>
    {
        public Task<BSValidationResult> ValidateAsync(TransactionAddSampleTask command, CancellationToken cancellationToken)
        {
            var errors = new List<BSValidationError>();

            ValidateName(command.Name, errors);
            ValidateTimer(command.TimerInMilliseconds, errors);

            return Task.FromResult(errors.Count == 0
                ? BSValidationResult.Valid()
                : BSValidationResult.Invalid(errors));
        }

        private static void ValidateName(string name, List<BSValidationError> errors)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(BSValidationError.Create(nameof(TransactionAddSampleTask.Name), "Nome é obrigatório"));
                return;
            }

            if (name.Trim().Length < 3)
            {
                errors.Add(BSValidationError.Create(nameof(TransactionAddSampleTask.Name), "Nome deve ter pelo menos 3 caracteres"));
            }

            if (name.Trim().Length > 100)
            {
                errors.Add(BSValidationError.Create(nameof(TransactionAddSampleTask.Name), "Nome não pode ter mais de 100 caracteres"));
            }
        }

        private static void ValidateTimer(int timer, List<BSValidationError> errors)
        {
            if (timer < 500)
            {
                errors.Add(BSValidationError.Create(nameof(TransactionAddSampleTask.TimerInMilliseconds), "Timer deve ser pelo menos 500ms"));
            }

            if (timer > 86400000) // 24 horas
            {
                errors.Add(BSValidationError.Create(nameof(TransactionAddSampleTask.TimerInMilliseconds), "Timer não pode exceder 24 horas"));
            }
        }
    }
}