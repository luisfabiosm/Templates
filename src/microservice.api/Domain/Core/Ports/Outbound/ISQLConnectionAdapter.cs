using System.Data;

namespace Domain.Core.Ports.Outbound
{
    public interface ISQLConnectionAdapter
    {

        void SetCorrelationId(string correlationId);
        Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

        Task CloseConnectionAsync();

        Task<T> ExecuteWithRetryAsync<T>(Func<IDbConnection, Task<T>> operation, CancellationToken cancellationToken = default);

        Task ExecuteWithRetryAsync(Func<IDbConnection, Task> operation, CancellationToken cancellationToken = default);

        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);

        Task<T> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);

        Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default);

    }
}
