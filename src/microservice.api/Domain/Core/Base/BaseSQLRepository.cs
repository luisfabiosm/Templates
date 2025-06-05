using Domain.Core.Base;
using Domain.Core.Interfaces.Outbound;
using Domain.Core.ResultPattern;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Domain.Core.Base
{
    /// <summary>
    /// Base repository SQL seguindo DRY e performance patterns
    /// Template Method pattern para reutilização
    /// </summary>
    public abstract class BaseSQLRepository<TEntity, TId> : IAsyncRepository<TEntity, TId>, IAsyncDisposable
    where TEntity : class
    where TId : struct
    {
        protected readonly ISQLConnectionAdapter _connectionAdapter;
        protected readonly ILogger _logger;
        private readonly SemaphoreSlim _semaphore;

        protected BaseSQLRepository(
            ISQLConnectionAdapter connectionAdapter,
            ILogger logger)
        {
            _connectionAdapter = connectionAdapter ?? throw new ArgumentNullException(nameof(connectionAdapter));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _semaphore = new SemaphoreSlim(1, 1);
        }

        public virtual async Task<BSResult<TEntity>> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                return await GetByIdInternalAsync(id, cancellationToken);
            }, cancellationToken);
        }

        public virtual async Task<BSResult<IReadOnlyList<TEntity>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                return await GetAllInternalAsync(cancellationToken);
            }, cancellationToken);
        }

        public virtual async Task<BSResult<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return await ExecuteWithRetryAsync(async () =>
            {
                return await AddInternalAsync(entity, cancellationToken);
            }, cancellationToken);
        }

        public virtual async Task<BSResult<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return await ExecuteWithRetryAsync(async () =>
            {
                return await UpdateInternalAsync(entity, cancellationToken);
            }, cancellationToken);
        }

        public virtual async Task<Result> DeleteAsync(TId id, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                return await DeleteInternalAsync(id, cancellationToken);
            }, cancellationToken);
        }

        public virtual async Task<BSResult<bool>> ExistsAsync(TId id, CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async () =>
            {
                return await ExistsInternalAsync(id, cancellationToken);
            }, cancellationToken);
        }

        // Template methods para implementação específica
        protected abstract Task<BSResult<TEntity>> GetByIdInternalAsync(TId id, CancellationToken cancellationToken);
        protected abstract Task<BSResult<IReadOnlyList<TEntity>>> GetAllInternalAsync(CancellationToken cancellationToken);
        protected abstract Task<BSResult<TEntity>> AddInternalAsync(TEntity entity, CancellationToken cancellationToken);
        protected abstract Task<BSResult<TEntity>> UpdateInternalAsync(TEntity entity, CancellationToken cancellationToken);
        protected abstract Task<Result> DeleteInternalAsync(TId id, CancellationToken cancellationToken);
        protected abstract Task<BSResult<bool>> ExistsInternalAsync(TId id, CancellationToken cancellationToken);

        private async Task<BSResult<T>> ExecuteWithRetryAsync<T>(
            Func<Task<BSResult<T>>> operation,
            CancellationToken cancellationToken,
            int maxRetries = 3)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                return await _connectionAdapter.ExecuteWithRetryAsync(async _ =>
                {
                    return await operation();
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar operação no repositório");
                return BSError.Internal($"Erro interno no repositório: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private async Task<Result> ExecuteWithRetryAsync(
            Func<Task<Result>> operation,
            CancellationToken cancellationToken,
            int maxRetries = 3)
        {
            var result = await ExecuteWithRetryAsync(async () =>
            {
                var operationResult = await operation();
                return operationResult.IsSuccess ? BSResult<bool>.Success(true) : BSResult<bool>.Failure(operationResult.Error);
            }, cancellationToken, maxRetries);

            return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore();
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            _semaphore?.Dispose();

            if (_connectionAdapter is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (_connectionAdapter is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

}
