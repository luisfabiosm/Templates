using Domain.Core.Common.Mediator;

namespace Domain.Core.Base
{

    public readonly record struct BaseTransaction<TResponse> : IBSRequest<TResponse>
    {
        public int Code { get; }
        public string CorrelationId { get; }

        public BaseTransaction(int code = 1, string? correlationId = null)
        {
            Code = code;
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }

        public static BaseTransaction<TResponse> Create(int code = 1)
            => new(code);

        public static BaseTransaction<TResponse> CreateWithCorrelationId(string correlationId, int code = 1)
            => new(code, correlationId);

        public BaseTransaction<TResponse> WithCode(int newCode)
            => new(newCode, CorrelationId);

        public BaseTransaction<TResponse> WithCorrelationId(string newCorrelationId)
            => new(Code, newCorrelationId);

        public override string ToString()
            => $"BaseTransaction(Code: {Code}, CorrelationId: {CorrelationId[..Math.Min(8, CorrelationId.Length)]})";
    }
}