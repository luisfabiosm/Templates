namespace Domain.Core.Mediator
{
    /// <summary>
    /// Mediator interface seguindo princípios SOLID
    /// Single Responsibility: apenas mediar requisições
    /// Interface Segregation: interface específica e focada
    /// </summary>
    public interface IBSMediator
    {
        Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IBSRequest<TResponse>;
    }
}
