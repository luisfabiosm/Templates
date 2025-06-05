using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Data;
using Dapper;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Settings;
using Domain.Core.Base;

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
    public class SQLConnectionAdapter : BaseService, ISQLConnectionAdapter, IAsyncDisposable
    {
        private readonly IOptions<DBSettings> _settings;
        private readonly AsyncRetryPolicy<IDbConnection> _retryPolicy;
        private DbConnection _connection;
        private readonly SemaphoreSlim _semaphore;
        private const int MaxRetries = 3;
        private string _CorrelationId;


        public SQLConnectionAdapter(IServiceProvider serviceProvider) : base (serviceProvider)
        {
            _settings = serviceProvider.GetRequiredService<IOptions<DBSettings>>();
            _retryPolicy = CreateRetryPolicy();
            _semaphore = new SemaphoreSlim(1, 1);
        }


        public void SetCorrelationId(string correlationId)
        {
            _CorrelationId = correlationId;
        }

        public string GetServer() => _settings.Value?.ServerUrl ?? throw new InvalidOperationException("Server não configurado");

        public async Task CloseConnectionAsync()
        {
            _loggingAdapter.LogInformation("Fechando conexão", _connection);

            if (_connection == null)
            {
                return;
            }

            await _semaphore.WaitAsync();
            try
            {
                var connection = _connection;

                if (connection != null)
                {

                    if (connection.State != ConnectionState.Closed)
                    {
                        await connection.CloseAsync();
                    }

                    await connection.DisposeAsync();
                }
            }
            catch (ObjectDisposedException)
            {
                // Ignore if connection is already disposed

            }
            finally
            {
                _connection = null;
                _semaphore.Release();
            }
        }

        public async Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return await _retryPolicy.ExecuteAsync(async () =>
                {
                    if (_connection == null || _connection.State != ConnectionState.Open)
                    {
                        await EnsureConnectionClosedAsync();
                        var _connectionString = _settings.Value.GetConnectionString();

                        if (string.IsNullOrEmpty(_connectionString))
                        {
                            var ex = new InvalidOperationException("Connectionstring não configurada");
                            throw ex;
                        }

                        var _newConnection = new SqlConnection(_connectionString);
                        _loggingAdapter.LogInformation("Nova conexão", _newConnection);
                        try
                        {
                            await _newConnection.OpenAsync(cancellationToken);
                            _connection = _newConnection;
                            _loggingAdapter.LogDebug("Database connection opened successfully");
                        }
                        catch (Exception ex)
                        {
                            _loggingAdapter.LogError("GetConnectionAsync: OpenAsync ", ex);
                            await EnsureConnectionClosedAsync();
                            throw;
                        }
                    }

                    return _connection;
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task ExecuteWithRetryAsync(Func<IDbConnection, Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync<object>(async (connection) =>
            {
                await operation(connection);
                return null;
            }, cancellationToken);
        }

        public async Task<T> ExecuteWithRetryAsync<T>(Func<IDbConnection, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {

                IDbConnection connection = null;
                {
                    try
                    {
                        connection = await GetConnectionAsync(cancellationToken);
                        if (connection.State != ConnectionState.Open)
                        {
                            _loggingAdapter.LogInformation("ExecuteWithRetryAsync Conexão encontrada fechada antes da operação. Tentando reabrir.");
                            await EnsureConnectionClosedAsync();
                            connection = await GetConnectionAsync(cancellationToken);
                        }

                        return await operation(connection);
                    }
                    catch (SqlException ex) when (IsTransientError(ex) && attempt < MaxRetries)
                    {
                        _loggingAdapter.LogError($"[ExecuteWithRetryAsync] Ocorreu um erro temporário (Tentativas {MaxRetries}): {ex.Message}",ex);
                        await Task.Delay(GetDelayMilliseconds(attempt), cancellationToken);
                        await EnsureConnectionClosedAsync();
                    }
                    catch (InvalidOperationException ex) when ((ex.Message.Contains("closed") || ex.Message.Contains("open")) && attempt < MaxRetries)
                    {
                        _loggingAdapter.LogError("ExecuteWithRetryAsync Conexão fechada inesperadamente",ex);
                        await Task.Delay(GetDelayMilliseconds(attempt), cancellationToken);
                        await EnsureConnectionClosedAsync();
                    }
                    throw new InvalidOperationException($"Operação falhou após {MaxRetries} tentativas");
                }
            }
            throw new InvalidOperationException($"A operação falhou após {MaxRetries} tentativas");
        }


        #region PRIVATE 

        private bool IsTransientError(DbException ex)
        {

#if SqlServerCondition

            int[] transientErrorNumbers = { -2, 10060, 10061, 1205, 50000 }; // Added 50000 for connection closed
            return transientErrorNumbers.Contains(((SqlException)ex).Number);

#elif PSQLCondition

            // PostgreSQL error codes for transient errors
            var transientErrorCodes = new[] { "08001", "08006", "40001", "40P01", "57014", "57P01", "57P02", "57P03" };
            return transientErrorCodes.Contains(((NpgsqlException)ex).SqlState);
#else
            return false;
#endif
        }

        private AsyncRetryPolicy<IDbConnection> CreateRetryPolicy()
        {
            return Policy<IDbConnection>
                .Handle<SqlException>(ex => IsTransientError(ex))
                .Or<InvalidOperationException>()
                .WaitAndRetryAsync(
                    MaxRetries,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    async (exception, duration, retryCount, context) =>
                    {
                        _loggingAdapter.LogWarning(
                            $"Falha na tentativa de conexão {retryCount}. Nova tentativa em 500ms. Erro: {exception.Exception}",
                            retryCount,
                            duration.TotalMilliseconds,
                            exception.Exception.Message);

                        await EnsureConnectionClosedAsync();
                    }
                );
        }

        private async Task EnsureConnectionClosedAsync()
        {
            if (_connection == null) return;

            var connectionToDispose = _connection;
            _connection = null;
            await DisposeConnectionAsync(connectionToDispose);
        }

        private async Task DisposeConnectionAsync(SqlConnection connection)
        {

            if (connection == null) return;

            try
            {
                if (connection.State != ConnectionState.Closed)
                {
                    await connection.CloseAsync();
                }
                await connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _loggingAdapter.LogError("Erro fechando conexão", ex);
            }
        }

        private int GetDelayMilliseconds(int attempt)
        {
            return (int)Math.Pow(2, attempt) * 500; // Exponential backoff: 2s, 4s, 8s...
        }


        #endregion

        #region Dapper Extension Methods for Convenience

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
            {
                return await connection.QueryAsync<T>(sql, param);
            }, cancellationToken);
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
            {
                return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
            }, cancellationToken);
        }

        public async Task<int> ExecuteAsync(string sql, object param = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
            {
                return await connection.ExecuteAsync(sql, param);
            }, cancellationToken);
        }

        #endregion


        public async ValueTask DisposeAsync()
        {

            await EnsureConnectionClosedAsync();
            _semaphore.Dispose();
            GC.SuppressFinalize(this);
        }

    }
}
