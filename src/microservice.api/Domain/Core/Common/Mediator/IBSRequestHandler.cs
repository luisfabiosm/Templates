namespace Domain.Core.Common.Mediator
{
    public interface IBSRequestHandler<in TRequest, TResponse> where TRequest : IBSRequest<TResponse>
    {
        ValueTask<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }
}
