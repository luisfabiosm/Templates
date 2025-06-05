
using MongoDB.Driver;
using Polly;
using Microsoft.Extensions.Options;
using Polly.Wrap;
using System.Net.Sockets;
using Adapters.Outbound.Logging;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.Settings;
using Domain.Core.Base;

namespace Adapters.Outbound.Database.NoSQL
{
    public class NoSQLConnectionAdapter : BaseService, INoSQLConnectionAdapter, IDisposable
    {
        private readonly IOptions<DBSettings> _settings;
        private readonly MongoClient _client;
        private readonly IMongoDatabase _database;
        private IClientSessionHandle _session;
        private readonly AsyncPolicyWrap _resiliencePolicy;
        private bool _disposed = false;
        private readonly string _databaseName;
        private string _CorrelationId;


        public NoSQLConnectionAdapter(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _settings = serviceProvider.GetRequiredService<IOptions<DBSettings>>();
            _resiliencePolicy = CreateResiliencePolicy(); 
            _client = new MongoClient(_settings.Value.GetNoSQLConnectionString());
            _database = _client.GetDatabase(_databaseName);
        }

        public void SetCorrelationId(string correlationId)
        {
            _CorrelationId = correlationId;
        }


        public IMongoCollection<T> GetCollection<T>(string collectionName)
        {
            return _database.GetCollection<T>(collectionName);
        }


        private AsyncPolicyWrap CreateResiliencePolicy()
        {
            // Política de retry - tenta 3 vezes com backoff exponencial
            var retryPolicy = Policy
                .Handle<MongoConnectionException>()
                .Or<TimeoutException>()
                .Or<SocketException>()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _loggingAdapter.LogError($"Tentativa {retryCount}: Erro ao conectar. Tentando novamente em {timeSpan.TotalSeconds} segundos. Erro: {exception.Message}", exception);                        
                    }
                );

            // Política de timeout - limita o tempo de espera para 30 segundos
            var timeoutPolicy = Policy.TimeoutAsync(30);

            // Circuit Breaker - abre o circuito após 5 falhas consecutivas
            var circuitBreakerPolicy = Policy
                .Handle<MongoConnectionException>()
                .Or<TimeoutException>()
                .CircuitBreakerAsync(
                    5,
                    TimeSpan.FromMinutes(1),
                    (ex, breakDuration) =>
                    {
                        _loggingAdapter.LogError($"Circuito aberto por {breakDuration.TotalMinutes} minutos devido a: {ex.Message}", ex);
                    },
                    () =>
                    {
                        _loggingAdapter.LogInformation("Circuito fechado. Conexões restabelecidas.");
                    }
                );

            // Combina as políticas (é executado de dentro para fora)
            return Policy.WrapAsync(retryPolicy, timeoutPolicy, circuitBreakerPolicy);
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    // Executa um comando simples para verificar se a conexão está funcionando
                    await _database.RunCommandAsync<dynamic>(new MongoDB.Bson.BsonDocument("ping", 1));
                    return true;
                });
            }
            catch (Exception ex)
            {
                _loggingAdapter.LogError($"Erro ao testar conexão: {ex.Message}", ex);
                return false;
            }
        }

        public async Task BeginTransactionAsync()
        {
            if (_session != null)
            {
                _loggingAdapter.LogError($"Uma transação já está em andamento");
                throw new InvalidOperationException("Uma transação já está em andamento");
            }

            _session = await _resiliencePolicy.ExecuteAsync(async () =>
                await _client.StartSessionAsync()
            );
            _session.StartTransaction();
        }


        public async Task CommitTransactionAsync()
        {
            if (_session == null)
            {
                _loggingAdapter.LogError($"Nenhuma transação em andamento");
                throw new InvalidOperationException("Nenhuma transação em andamento");
            }

            await _resiliencePolicy.ExecuteAsync(async () =>
            {
                await _session.CommitTransactionAsync();
            });

            _session.Dispose();
            _session = null;
        }


        public async Task AbortTransactionAsync()
        {
            if (_session == null)
            {
                _loggingAdapter.LogError($"Nenhuma transação em andamento");
                throw new InvalidOperationException("Nenhuma transação em andamento");
            }

            await _resiliencePolicy.ExecuteAsync(async () =>
            {
                await _session.AbortTransactionAsync();
            });

            _session.Dispose();
            _session = null;
        }


        public async Task<T> ExecuteAsync<T>(Func<IClientSessionHandle, Task<T>> operation, bool useTransaction = true)
        {
            bool createdNewTransaction = false;

            try
            {
                if (useTransaction && _session == null)
                {
                    await BeginTransactionAsync();
                    createdNewTransaction = true;
                }

                // Executa a operação com as políticas de resiliência
                return await _resiliencePolicy.ExecuteAsync(async () =>
                {
                    if (useTransaction)
                    {
                        return await operation(_session);
                    }
                    else
                    {
                        // Operação sem transação
                        using var tempSession = await _client.StartSessionAsync();
                        return await operation(tempSession);
                    }
                });
            }
            catch (Exception)
            {
                // Em caso de erro, aborta a transação se tiver sido criada aqui
                if (createdNewTransaction && _session != null)
                {
                    await AbortTransactionAsync();
                }
                throw;
            }
            finally
            {
                // Confirma a transação se tiver sido criada aqui
                if (createdNewTransaction && _session != null)
                {
                    await CommitTransactionAsync();
                }
            }
        }

        public async Task ExecuteAsync(Func<IClientSessionHandle, Task> operation, bool useTransaction = true)
        {
            await ExecuteAsync(async (session) =>
            {
                await operation(session);
                return true;
            }, useTransaction);
        }


        public async Task<TResult> QueryAsync<T, TResult>(string collectionName, Func<IMongoCollection<T>, Task<TResult>> queryOperation)
        {
            var collection = GetCollection<T>(collectionName);
            return await _resiliencePolicy.ExecuteAsync(async () => await queryOperation(collection));
        }

 


        #region Dispose

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _session?.Dispose();
                }

                _disposed = true;
            }
        }

        #endregion
    }
}