using Domain.Core.Models.Transaction;
using System.Transactions;

namespace Domain.Core.Base
{
    /// <summary>
    /// Interface segregada para transações seguindo ISP
    /// </summary>
    public interface ITransaction
    {
        TransactionCode Code { get; }
        CorrelationId CorrelationId { get; }
    }
}
