using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Core.Mediator
{
    /// <summary>
    /// Mediator implementation seguindo KISS e Object Calisthenics
    /// - Apenas um nível de indentação
    /// - Sem else keywords
    /// - Nomes descritivos completos
    /// - Responsabilidade única: mediar requests
    /// </summary>
    public sealed class BSMediator : IBSMediator
    {
        private readonly IServiceProvider _serviceProvider;

        public BSMediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IBSRequest<TResponse>
        {
            ArgumentNullException.ThrowIfNull(request);

            var handler = ResolveHandler<TRequest, TResponse>();
            return await handler.Handle(request, cancellationToken).ConfigureAwait(false);
        }

        private IBSRequestHandler<TRequest, TResponse> ResolveHandler<TRequest, TResponse>()
            where TRequest : IBSRequest<TResponse>
        {
            var handlerType = typeof(IBSRequestHandler<TRequest, TResponse>);
            var handler = _serviceProvider.GetService<IBSRequestHandler<TRequest, TResponse>>();

            if (handler is null)
            {
                ThrowHandlerNotFoundException<TRequest>();
            }

            return handler;
        }

        private static void ThrowHandlerNotFoundException<TRequest>()
        {
            throw new InvalidOperationException($"Handler não encontrado para {typeof(TRequest).Name}");
        }
    }
}