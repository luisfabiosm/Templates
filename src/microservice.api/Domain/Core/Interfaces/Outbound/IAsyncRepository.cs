
using Domain.Core.ResultPattern;

namespace Domain.Core.Interfaces.Outbound
{
    public interface IAsyncRepository<TEntity, TId>
         where TEntity : class
         where TId : struct
    {
        Task<BSResult<TEntity>> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
        Task<BSResult<IReadOnlyList<TEntity>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<BSResult<TEntity>> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<BSResult<TEntity>> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(TId id, CancellationToken cancellationToken = default);
        Task<BSResult<bool>> ExistsAsync(TId id, CancellationToken cancellationToken = default);
    }
}
