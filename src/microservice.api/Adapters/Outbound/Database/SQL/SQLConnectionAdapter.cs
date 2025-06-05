using Dapper;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
    /// <summary>
    /// Adaptador de conexão SQL com pool de conexões e retry policy
    /// Seguindo princípios de performance e thread safety
    /// </summary>
    public sealed class SQLConnectionAdapter : ISQLConnectionAdapter
    {
        private readonly IOptions<DBSettings> _settings;
        private readonly ILogger<SQLConnectionAdapter> _logger;
        private readonly IAsyncPolicy _retryPolicy;
        private readonly ConnectionPool _connectionPool;
        private readonly SemaphoreSlim _semaphore;

        private const int MaxRetries = 3;
        private const int MaxPoolSize = 10;

        public SQLConnectionAdapter(
            IOptions<DBSettings> settings,
            ILogger<SQLConnectionAdapter> logger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _retryPolicy = CreateRetryPolicy();
            _connectionPool = new ConnectionPool(MaxPoolSize, CreateConnection);
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
            {
                return await connection.QueryAsync<T>(sql, param);
            }, cancellationToken);
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
            {
                return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
            }, cancellationToken);
        }

        public async Task<int> ExecuteAsync(string sql, object? param = null, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
            {
                return await connection.ExecuteAsync(sql, param);
            }, cancellationToken);
        }

        public async Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            return await _connectionPool.GetConnectionAsync(cancellationToken);
        }

        public async Task CloseConnectionAsync()
        {
            await _connectionPool.DisposeAsync();
        }

        public async Task<T> ExecuteWithRetryAsync<T>(Func<IDbConnection, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);

            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var connection = await _connectionPool.GetConnectionAsync(cancellationToken);
                try
                {
                    await EnsureConnectionOpenAsync(connection, cancellationToken);
                    return await operation(connection);
                }
                finally
                {
                    await _connectionPool.ReturnConnectionAsync(connection);
                }
            });
        }

        public async Task ExecuteWithRetryAsync(Func<IDbConnection, Task> operation, CancellationToken cancellationToken = default)
        {
            await ExecuteWithRetryAsync(async connection =>
            {
                await operation(connection);
                return 0; // Valor dummy para converter Task em Task<T>
            }, cancellationToken);
        }

        private IAsyncPolicy CreateRetryPolicy()
        {
            return Policy
                .Handle<System.Data.Common.DbException>(IsTransientError)
                .Or<InvalidOperationException>(ex => ex.Message.Contains("closed") || ex.Message.Contains("open"))
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Tentativa {RetryCount} falhou. Nova tentativa em {Delay}s. Erro: {Error}",
                            retryCount,
                            timespan.TotalSeconds,
                            outcome.Exception?.Message);
                    });
        }

        private static bool IsTransientError(System.Data.Common.DbException ex)
        {
#if SqlServerCondition
            // SQL Server transient error numbers
            if (ex is SqlException sqlEx)
            {
                int[] transientErrorNumbers = { -2, 2, 20, 64, 233, 10053, 10054, 10060, 40197, 40501, 40613 };
                return transientErrorNumbers.Contains(sqlEx.Number);
            }
#elif PSQLCondition
            // PostgreSQL transient error codes
            if (ex is NpgsqlException npgsqlEx && !string.IsNullOrEmpty(npgsqlEx.SqlState))
            {
                var transientErrorCodes = new[] { "08001", "08006", "40001", "40P01", "57014", "57P01", "57P02", "57P03" };
                return transientErrorCodes.Contains(npgsqlEx.SqlState);
            }
#endif
            return false;
        }

        private DbConnection CreateConnection()
        {
            var connectionString = _settings.Value.GetConnectionString();
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string não configurada");
            }

#if SqlServerCondition
            return new SqlConnection(connectionString);
#elif PSQLCondition
            return new NpgsqlConnection(connectionString);
#else
            throw new NotSupportedException("Database type não suportado");
#endif
        }

        private static async Task EnsureConnectionOpenAsync(IDbConnection connection, CancellationToken cancellationToken)
        {
            if (connection.State != ConnectionState.Open)
            {
                if (connection is DbConnection dbConnection)
                {
                    await dbConnection.OpenAsync(cancellationToken);
                }
                else
                {
                    connection.Open();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _connectionPool.DisposeAsync();
            _semaphore?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Pool de conexões thread-safe para reutilização
    /// Implementação usando Channel para alta performance
    /// </summary>
    internal sealed class ConnectionPool : IAsyncDisposable
    {
        private readonly Channel<DbConnection> _connections;
        private readonly ChannelWriter<DbConnection> _writer;
        private readonly ChannelReader<DbConnection> _reader;
        private readonly Func<DbConnection> _connectionFactory;
        private readonly SemaphoreSlim _semaphore;
        private readonly int _maxSize;
        private volatile bool _disposed;

        public ConnectionPool(int maxSize, Func<DbConnection> connectionFactory)
        {
            _maxSize = maxSize;
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

            var options = new BoundedChannelOptions(maxSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };

            _connections = Channel.CreateBounded<DbConnection>(options);
            _writer = _connections.Writer;
            _reader = _connections.Reader;
            _semaphore = new SemaphoreSlim(maxSize, maxSize);

            // Pre-populate pool
            _ = Task.Run(InitializePoolAsync);
        }

        public async Task<DbConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_reader.TryRead(out var connection))
                {
                    if (IsConnectionValid(connection))
                    {
                        return connection;
                    }

                    // Connection is invalid, dispose and create new one
                    await DisposeConnectionAsync(connection);
                }

                // Create new connection if pool is empty or connection was invalid
                return _connectionFactory();
            }
            catch
            {
                _semaphore.Release();
                throw;
            }
        }

        public async Task ReturnConnectionAsync(DbConnection connection)
        {
            ThrowIfDisposed();

            try
            {
                if (IsConnectionValid(connection))
                {
                    if (!_writer.TryWrite(connection))
                    {
                        // Pool is full, dispose the connection
                        await DisposeConnectionAsync(connection);
                    }
                }
                else
                {
                    await DisposeConnectionAsync(connection);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task InitializePoolAsync()
        {
            for (int i = 0; i < _maxSize; i++)
            {
                try
                {
                    var connection = _connectionFactory();
                    if (!_writer.TryWrite(connection))
                    {
                        await DisposeConnectionAsync(connection);
                        break;
                    }
                }
                catch
                {
                    // Ignore errors during initialization
                }
            }
        }

        private static bool IsConnectionValid(DbConnection connection)
        {
            return connection?.State == ConnectionState.Open || connection?.State == ConnectionState.Closed;
        }

        private static async Task DisposeConnectionAsync(DbConnection connection)
        {
            try
            {
                if (connection?.State == ConnectionState.Open)
                {
                    await connection.CloseAsync();
                }
                connection?.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(ConnectionPool));
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            _disposed = true;
            _writer.Complete();

            await foreach (var connection in _reader.ReadAllAsync())
            {
                await DisposeConnectionAsync(connection);
            }

            _semaphore?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}