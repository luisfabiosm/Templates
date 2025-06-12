using MongoDB.Driver;

namespace Domain.Core.Ports.Outbound
{
    public interface INoSQLConnectionAdapter 
    {

        void SetCorrelationId(string correlationId);

        IMongoCollection<T> GetCollection<T>(string collectionName);

        Task<bool> TestConnectionAsync();
        Task BeginTransactionAsync();

        Task CommitTransactionAsync();

        Task AbortTransactionAsync();

        Task<T> ExecuteAsync<T>(Func<IClientSessionHandle, Task<T>> operation, bool useTransaction = true);

        Task ExecuteAsync(Func<IClientSessionHandle, Task> operation, bool useTransaction = true);

        Task<TResult> QueryAsync<T, TResult>(string collectionName, Func<IMongoCollection<T>, Task<TResult>> queryOperation);

        void Dispose();
    }
}
