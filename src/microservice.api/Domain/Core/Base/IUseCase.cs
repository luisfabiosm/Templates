namespace Domain.Core.Base
{
    /// <summary>
    /// Interface base para use cases seguindo ISP
    /// </summary>
    public interface IUseCase<in TRequest, TResponse>
    {
        Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
    }
}
