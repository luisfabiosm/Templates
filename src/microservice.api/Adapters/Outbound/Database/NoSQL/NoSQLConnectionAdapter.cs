
using MongoDB.Driver;
using Polly;
using Domain.Core.Base;
using Microsoft.Extensions.Options;
using Polly.Wrap;
using System.Net.Sockets;
using Adapters.Outbound.Logging;
using Domain.Core.Settings;
using Domain.Core.Ports.Outbound;

namespace Adapters.Outbound.Database.NoSQL
{
    public class NoSQLConnectionAdapter : BaseService, INoSQLConnectionAdapter, IDisposable
    {
        private readonly IOptions<DBSettings> _settings;
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        private readonly AsyncPolicyWrap _resiliencePolicy;
        private readonly SemaphoreSlim _sessionSemaphore;
        private readonly object _lockObject = new();

        private volatile bool _disposed;
        private IClientSessionHandle? _session;
        private string _correlationId = string.Empty;

        private const int MaxRetries = 3;
        private const int CircuitBreakerThreshold = 5;
        private const int TimeoutSeconds = 30;

        public NoSQLConnectionAdapter(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _settings = serviceProvider.GetRequiredService<IOptions<DBSettings>>();
            _resiliencePolicy = CreateResiliencePolicy();
            _sessionSemaphore = new SemaphoreSlim(1, 1);

            var connectionString = _settings.Value.GetNoSQLConnectionString();
            _client = new MongoClient(connectionString);
            _database = _client.GetDatabase(_settings.Value.Database);

            _loggingAdapter.LogDebug("NoSQL Connection Adapter inicializado para database: {Database}",
                _settings.Value.Database);
        }

        public void SetCorrelationId(string correlationId)
        {
            _correlationId = correlationId ?? string.Empty;
        }

        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);

            return _database.GetCollection<T>(collectionName);
        }


        public async Task<bool> TestConnectionAsync()
        {
            ThrowIfDisposed();

            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    await _database.RunCommandAsync<dynamic>(new MongoDB.Bson.BsonDocument("ping", 1));
                    _loggingAdapter.LogDebug("Teste de conexão NoSQL bem-sucedido");
                    return true;
                });
            }
            catch (Exception ex)
            {
                _loggingAdapter.LogError("Erro ao testar conexão NoSQL: {Error}", ex, ex.Message);
                return false;
            }
        }

        public async Task BeginTransactionAsync()
        {
            ThrowIfDisposed();

            await _sessionSemaphore.WaitAsync();
            try
            {
                if (_session != null)
                {
                    _loggingAdapter.LogWarning("Uma transação já está em andamento para CorrelationId: {CorrelationId}",
                        _correlationId);
                    throw new InvalidOperationException("Uma transação já está em andamento");
                }

                _session = await _resiliencePolicy.ExecuteAsync(async () =>
                    await _client.StartSessionAsync());

                _session.StartTransaction();
                _loggingAdapter.LogDebug("Transação NoSQL iniciada com sucesso");
            }
            finally
            {
                _sessionSemaphore.Release();
            }
        }

        public async Task CommitTransactionAsync()
        {
            ThrowIfDisposed();

            await _sessionSemaphore.WaitAsync();
            try
            {
                if (_session == null)
                {
                    _loggingAdapter.LogWarning("Tentativa de commit sem transação ativa para CorrelationId: {CorrelationId}",
                        _correlationId);
                    throw new InvalidOperationException("Nenhuma transação em andamento");
                }

                await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    await _session.CommitTransactionAsync();
                });

                _loggingAdapter.LogDebug("Transação NoSQL commitada com sucesso");
            }
            finally
            {
                await DisposeCurrentSessionSafelyAsync();
                _sessionSemaphore.Release();
            }
        }

        public async Task AbortTransactionAsync()
        {
            ThrowIfDisposed();

            await _sessionSemaphore.WaitAsync();
            try
            {
                if (_session == null)
                {
                    _loggingAdapter.LogWarning("Tentativa de abort sem transação ativa para CorrelationId: {CorrelationId}",
                        _correlationId);
                    throw new InvalidOperationException("Nenhuma transação em andamento");
                }

                await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    await _session.AbortTransactionAsync();
                });

                _loggingAdapter.LogDebug("Transação NoSQL abortada com sucesso");
            }
            finally
            {
                await DisposeCurrentSessionSafelyAsync();
                _sessionSemaphore.Release();
            }
        }

        public async Task<T> ExecuteAsync<T>(Func<IClientSessionHandle, Task<T>> operation, bool useTransaction = true)
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(operation);

            var createdNewTransaction = false;

            try
            {
                if (useTransaction && _session == null)
                {
                    await BeginTransactionAsync();
                    createdNewTransaction = true;
                }

                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    if (useTransaction && _session != null)
                    {
                        return await operation(_session);
                    }

                    // Operação sem transação - usa sessão temporária
                    using var tempSession = await _client.StartSessionAsync();
                    return await operation(tempSession);
                });
            }
            catch (Exception ex)
            {
                _loggingAdapter.LogError("Erro durante execução de operação NoSQL: {Error}", ex, ex.Message);

                if (createdNewTransaction && _session != null)
                {
                    await AbortTransactionAsync();
                }
                throw;
            }
            finally
            {
                if (createdNewTransaction && _session != null)
                {
                    await CommitTransactionAsync();
                }
            }
        }

        public async Task ExecuteAsync(Func<IClientSessionHandle, Task> operation, bool useTransaction = true)
        {
            await ExecuteAsync(async session =>
            {
                await operation(session);
                return true;
            }, useTransaction);
        }

        public async Task<TResult> QueryAsync<T, TResult>(string collectionName, Func<IMongoCollection<T>, Task<TResult>> queryOperation)
        {
            ThrowIfDisposed();
            ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
            ArgumentNullException.ThrowIfNull(queryOperation);

            var collection = GetCollection<T>(collectionName);

            return await _resiliencePolicy.ExecuteAsync(async () =>
            {
                _loggingAdapter.LogDebug("Executando query na collection: {Collection}", collectionName);
                return await queryOperation(collection);
            });
        }

        #region Private Methods

        private AsyncPolicyWrap CreateResiliencePolicy()
        {
            var retryPolicy = Policy
                .Handle<MongoConnectionException>()
                .Or<TimeoutException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(
                    MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _loggingAdapter.LogWarning(
                            "Tentativa {RetryCount}: Erro ao conectar NoSQL. Tentando novamente em {TimeSpan} segundos. Erro: {Error}",
                            retryCount, timeSpan.TotalSeconds, exception.Message);
                    }
                );

            var timeoutPolicy = Policy.TimeoutAsync(TimeoutSeconds);

            var circuitBreakerPolicy = Policy
                .Handle<MongoConnectionException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    CircuitBreakerThreshold,
                    TimeSpan.FromMinutes(1),
                    (ex, breakDuration) =>
                    {
                        _loggingAdapter.LogError(
                            "Circuit Breaker NoSQL aberto por {BreakDuration} minutos devido a: {Error}",
                            ex, breakDuration.TotalMinutes, ex.Message);
                    },
                    () =>
                    {
                        _loggingAdapter.LogInformation("Circuit Breaker NoSQL fechado. Conexões restabelecidas");
                    }
                );

            return Policy.WrapAsync(retryPolicy, timeoutPolicy, circuitBreakerPolicy);
        }

        private async Task DisposeCurrentSessionSafelyAsync()
        {
            if (_session == null) return;

            var sessionToDispose = _session;
            _session = null;

            try
            {
                sessionToDispose.Dispose();
                await Task.CompletedTask; // Para manter a assinatura async
            }
            catch (Exception ex)
            {
                _loggingAdapter.LogWarning("Erro ao fazer dispose da sessão NoSQL: {Error}", ex.Message);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NoSQLConnectionAdapter));
            }
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
                DisposeCurrentSessionSafelyAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _loggingAdapter.LogError("Erro durante dispose da sessão NoSQL: {Error}", ex, ex.Message);
            }
            finally
            {
                _sessionSemaphore.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}