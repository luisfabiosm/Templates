using Domain.Core.Mediator;
using Domain.Core.Models.Transaction;

namespace Domain.Core.Base
{
    /// <summary>
    /// Base transaction seguindo Object Calisthenics
    /// - Máximo duas variáveis de instância
    /// - Sem getters/setters desnecessários
    /// - Nomes descritivos completos
    /// </summary>
    public abstract record BaseTransaction<TResponse> : IBSRequest<TResponse>, ITransaction
    {
        public TransactionCode Code { get; init; }
        public CorrelationId CorrelationId { get; init; }

        protected BaseTransaction(int code)
        {
            Code = new TransactionCode(code);
            CorrelationId = new CorrelationId();
        }

        protected BaseTransaction(int code, string correlationId)
        {
            Code = new TransactionCode(code);
            CorrelationId = new CorrelationId(correlationId);
        }
    }
}