using System.Threading;
using System.Threading.Tasks;

namespace Domain.Core.Mediator
{
    /// <summary>
    /// Handler interface seguindo Interface Segregation Principle
    /// Apenas uma responsabilidade: processar requests
    /// </summary>
    public interface IBSRequestHandler<in TRequest, TResponse>
        where TRequest : IBSRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}