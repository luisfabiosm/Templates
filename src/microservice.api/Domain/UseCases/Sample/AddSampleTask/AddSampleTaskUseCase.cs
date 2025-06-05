using Domain.Core.Base;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Models.Entity;
using Domain.Core.Models.Responses;
using Domain.Core.Models.ValueObjects;
using Domain.Core.ResultPattern;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Domain.UseCases.Sample.AddSampleTask
{
    /// <summary>
    /// Use case para adicionar SampleTask seguindo Clean Architecture
    /// </summary>
    public sealed class AddSampleTaskUseCase : BaseUseCaseHandler<TransactionAddSampleTask, BSResult<ResponseNewSampleTask>>
    {
        private readonly ISampleTaskRepository _repository;

        public AddSampleTaskUseCase(
            ILoggingAdapter logger,
            IValidator<TransactionAddSampleTask> validator,
            ISampleTaskRepository repository)
            : base(logger, validator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        protected override async Task<BSResult<ResponseNewSampleTask>> ExecuteUseCaseAsync(
            TransactionAddSampleTask request,
            CancellationToken cancellationToken)
        {
            var sampleTask = SampleTask.Create(
                new TaskName(request.Name),
            new TimerInMilliseconds(request.TimerInMilliseconds));

            var result = await _repository.AddAsync(sampleTask, cancellationToken);

            return result.Match(
                success => BSResult<ResponseNewSampleTask>.Success(new ResponseNewSampleTask(success)),
                error => BSResult<ResponseNewSampleTask>.Failure(error));
        }

        protected override BSResult<ResponseNewSampleTask> CreateValidationErrorResponse(
        BSValidationResult validationResult,
            string correlationId)
        {
            var error = BSError.Validation("Dados de entrada inválidos");
            return BSResult<ResponseNewSampleTask>.Failure(error);
        }

        protected override BSResult<ResponseNewSampleTask> CreateErrorResponse(
        Exception exception,
            string correlationId)
        {
            var error = BSError.Internal($"Erro interno: {exception.Message}");
            return BSResult<ResponseNewSampleTask>.Failure(error);
        }
    }
}