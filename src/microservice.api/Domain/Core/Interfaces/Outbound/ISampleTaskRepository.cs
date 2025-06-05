using Domain.Core.Models.Entity;
using Domain.Core.Models.ValueObjects;
using Domain.Core.ResultPattern;

namespace Domain.Core.Interfaces.Outbound
{
    /// <summary>
    /// Repository específico para SampleTask seguindo ISP
    /// Apenas operações específicas para este agregado
    /// </summary>
    public interface ISampleTaskRepository : IAsyncRepository<SampleTask, SampleTaskId>
    {
        Task<BSResult<IReadOnlyList<SampleTask>>> GetByTimerRangeAsync(
            int minTimer,
            int maxTimer,
            CancellationToken cancellationToken = default);

        Task<BSResult<IReadOnlyList<SampleTask>>> GetActiveTasksAsync(
            CancellationToken cancellationToken = default);

        Task<Result> UpdateTimerAsync(
            SampleTaskId id,
            TimerInMilliseconds timer,
            CancellationToken cancellationToken = default);
    }
}
