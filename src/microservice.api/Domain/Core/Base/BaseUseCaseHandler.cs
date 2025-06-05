using Domain.Core.Interfaces.Outbound;
using Domain.Core.Mediator;
using Domain.Core.ResultPattern;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Core.Base
{
    public abstract class BaseUseCaseHandler<TRequest, TResponse> : IBSRequestHandler<TRequest, TResponse>
           where TRequest : IBSRequest<TResponse>
    {
        protected readonly ILoggingAdapter _logger;
        protected readonly IValidator<TRequest> _validator;

        protected BaseUseCaseHandler(
            ILoggingAdapter logger,
            IValidator<TRequest> validator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var correlationId = GetCorrelationId(request);

            using var operation = _logger.StartOperation(GetOperationName(), correlationId);

            try
            {
                _logger.LogInformation("Iniciando processamento: {RequestType}", typeof(TRequest).Name);

                var validationResult = await ValidateRequestAsync(request, cancellationToken);
                if (validationResult.IsInvalid)
                {
                    return CreateValidationErrorResponse(validationResult, correlationId);
                }

                var result = await ExecuteUseCaseAsync(request, cancellationToken);

                _logger.LogInformation("Processamento concluído com sucesso: {RequestType}", typeof(TRequest).Name);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Erro durante processamento: {RequestType} - {Error}", ex, ex.Message, ex);
                return CreateErrorResponse(ex, correlationId);
            }
        }

        protected abstract Task<TResponse> ExecuteUseCaseAsync(TRequest request, CancellationToken cancellationToken);
        protected abstract TResponse CreateValidationErrorResponse(BSValidationResult validationResult, string correlationId);
        protected abstract TResponse CreateErrorResponse(Exception exception, string correlationId);

        protected virtual async Task<BSValidationResult> ValidateRequestAsync(TRequest request, CancellationToken cancellationToken)
        {
            return await _validator.ValidateAsync(request, cancellationToken);
        }

        protected virtual string GetOperationName() => GetType().Name;

        protected virtual string GetCorrelationId(TRequest request)
        {
            return request switch
            {
                ITransaction transaction => transaction.CorrelationId.Value,
                _ => Guid.NewGuid().ToString()
            };
        }
    }
}
