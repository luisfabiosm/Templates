using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;


namespace Domain.Core.Interfaces.Outbound
{
    public interface ISQLConnectionAdapter : IAsyncDisposable
    {
        Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
        Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
        Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default);
        Task<T> ExecuteWithRetryAsync<T>(Func<IDbConnection, Task<T>> operation, CancellationToken cancellationToken = default);
        Task ExecuteWithRetryAsync(Func<IDbConnection, Task> operation, CancellationToken cancellationToken = default);
        Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
        Task CloseConnectionAsync();
    }
}
