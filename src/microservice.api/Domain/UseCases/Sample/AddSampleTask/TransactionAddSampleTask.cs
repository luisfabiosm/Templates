
using Domain.Core.Base;
using Domain.Core.Models.Responses;
using Domain.Core.ResultPattern;

namespace Domain.UseCases.Sample.AddSampleTask
{
    /// <summary>
    /// Command para adicionar SampleTask seguindo CQRS
    /// </summary>
    public sealed record TransactionAddSampleTask : BaseTransaction<BSResult<ResponseNewSampleTask>>
    {
        public string Name { get; }
        public int TimerInMilliseconds { get; }

        public TransactionAddSampleTask(string name, int timerInMilliseconds)
            : base(code: 1)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            TimerInMilliseconds = timerInMilliseconds;
        }
    }
}