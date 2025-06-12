
using Domain.Core.Base;
using Domain.Core.Settings;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Data;
using System.Data.Common;
using Dapper;
using Domain.Core.Ports.Outbound;
using System.Threading;


#if SqlServerCondition

using Microsoft.Data.SqlClient;
using DbConnection = Microsoft.Data.SqlClient.SqlConnection;
using DbException = Microsoft.Data.SqlClient.SqlException;

#elif PSQLCondition

using Npgsql;
using DbConnection = Npgsql.NpgsqlConnection;
using DbException = Npgsql.NpgsqlException;

#endif


namespace Adapters.Outbound.Database.SQL
{
    public class SQLConnectionAdapter : BaseService, ISQLConnectionAdapter, IDisposable
    {
        private readonly IOptions<DBSettings> _settings;
        private readonly AsyncRetryPolicy<IDbConnection> _retryPolicy;
        private readonly SemaphoreSlim _connectionSemaphore;
        private readonly object _lockObject = new();

        private const int MaxRetries = 3;
        private const int BaseDelayMs = 500;

        private volatile bool _disposed;
        private DbConnection? _connection;
        private string _correlationId = string.Empty;

        public SQLConnectionAdapter(IServiceProvider serviceProvider) : base (serviceProvider)
        {
            _settings = serviceProvider.GetRequiredService<IOptions<DBSettings>>();
            _retryPolicy = CreateRetryPolicy();
            _connectionSemaphore = new SemaphoreSlim(1, 1);
        }

        public void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId ?? string.Empty;
        }

        public async Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _connectionSemaphore.WaitAsync(cancellationToken);
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (ShouldCreateNewConnection())
                    {
                        await CloseCurrentConnectionSafelyAsync();
                        await CreateNewConnectionAsync(cancellationToken);
                    }

                    return _connection!;
                });
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }


        public async Task CloseConnectionAsync()
        {
            if (_disposed) return;

            await _connectionSemaphore.WaitAsync();
            try
            {
                await CloseCurrentConnectionSafelyAsync();
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        public async Task<T> ExecuteWithRetryAsync<T>(Func<IDbConnection, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(operation);

            for (var attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var connection = await GetConnectionAsync(cancellationToken);
                    ValidateConnectionState(connection);

                    return await operation(connection);
                }
                catch (Exception ex) when (ShouldRetry(ex, attempt))
                {
                    _loggingAdapter.LogWarning(
                        "Tentativa {Attempt}/{MaxRetries} falhou. Erro: {Error}. Tentando novamente...",
                        attempt, MaxRetries, ex.Message);

                    await Task.Delay(CalculateDelay(attempt), cancellationToken);
                    await CloseConnectionAsync();
                }
            }

            throw new InvalidOperationException($"Operação falhou após {MaxRetries} tentativas");
        }

        public async Task ExecuteWithRetryAsync(Func<IDbConnection, Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync(async connection =>
            {
                await operation(connection);
                return true;
            }, cancellationToken);
        }

        #region Private Methods

        private bool ShouldCreateNewConnection()
        {
            return _connection?.State != ConnectionState.Open;
        }

        private async Task CreateNewConnectionAsync(CancellationToken cancellationToken)
        {
            var connectionString = _settings.Value.GetConnectionString();

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string não configurada");
            }

            _connection = new DbConnection(connectionString);
            _loggingAdapter.LogDebug("Nova conexão criada para CorrelationId: {CorrelationId}", _correlationId);

            await _connection.OpenAsync(cancellationToken);
            _loggingAdapter.LogDebug("Conexão aberta com sucesso");
        }

        private async Task CloseCurrentConnectionSafelyAsync()
        {
            if (_connection == null) return;

            var connectionToDispose = _connection;
            _connection = null;

            try
            {
                if (connectionToDispose.State != ConnectionState.Closed)
                {
                    await connectionToDispose.CloseAsync();
                }
            }
            catch (Exception ex)
            {
                _loggingAdapter.LogWarning("Erro ao fechar conexão: {Error}", ex.Message);
            }
            finally
            {
                await connectionToDispose.DisposeAsync();
            }
        }

        private static void ValidateConnectionState(IDbConnection connection)
        {
            if (connection.State != ConnectionState.Open)
            {
                throw new InvalidOperationException("Conexão não está aberta para execução");
            }
        }

        private bool ShouldRetry(Exception exception, int attempt)
        {
            return attempt < MaxRetries && (IsTransientError(exception) || IsConnectionError(exception));
        }

        private bool IsTransientError(Exception exception)
        {
            return exception switch
            {
#if SqlServerCondition
                SqlException sqlEx => new[] { -2, 10060, 10061, 1205, 50000 }.Contains(sqlEx.Number),
#elif PSQLCondition
                NpgsqlException npgsqlEx => new[] { "08001", "08006", "40001", "40P01", "57014", "57P01", "57P02", "57P03" }
                    .Contains(npgsqlEx.SqlState),
#endif
                _ => false
            };
        }

        private static bool IsConnectionError(Exception exception)
        {
            return exception is InvalidOperationException &&
                   (exception.Message.Contains("closed") || exception.Message.Contains("open"));
        }

        private AsyncRetryPolicy<IDbConnection> CreateRetryPolicy()
        {
            return Policy<IDbConnection>
                .Handle<DbException>(IsTransientError)
                .Or<InvalidOperationException>(IsConnectionError)
                .WaitAndRetryAsync(
                    MaxRetries,
                    attempt => TimeSpan.FromMilliseconds(CalculateDelay(attempt)),
                    async (exception, duration, retryCount, context) =>
                    {
                        _loggingAdapter.LogWarning(
                            "Falha na conexão - Tentativa {RetryCount}. Nova tentativa em {Duration}ms. Erro: {Error}",
                            retryCount, duration.TotalMilliseconds, exception.Exception.Message);

                        await CloseCurrentConnectionSafelyAsync();
                    });
        }

        private static int CalculateDelay(int attempt)
        {
            return (int)Math.Pow(2, attempt) * BaseDelayMs; // Exponential backoff
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SQLConnectionAdapter));
            }
        }

        #endregion


        #region Dapper Extension Methods

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
                await connection.QueryAsync<T>(sql, param), cancellationToken);
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            return (await ExecuteWithRetryAsync(async connection =>
                await connection.QueryFirstOrDefaultAsync<T>(sql, param), cancellationToken))!;
        }

        public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
                await connection.ExecuteAsync(sql, param), cancellationToken);
        }

        #endregion
        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            lock (_lockObject)
            {
                if (_disposed) return;
                _disposed = true;
            }

            try
            {
                CloseCurrentConnectionSafelyAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _loggingAdapter.LogError("Erro durante dispose da conexão: {Error}", ex, ex.Message);
            }
            finally
            {
                _connectionSemaphore.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        #endregion

    }
}
