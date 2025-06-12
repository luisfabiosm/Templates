using Domain.Core.Models.Dto;
using Domain.Core.Models.Entity;
using Domain.UseCases.Sample.AddSampleTask;
using Domain.UseCases.Sample.GetSampleTask;
using Domain.UseCases.Sample.ListSampleTask;
using Domain.UseCases.Sample.UpdateSampleTaskTimer;

namespace Domain.Core.Ports.Outbound
{
    public interface ISQLSampleRepository
    {


        ValueTask<List<SampleTask>> ListAllSampleTaskAsync(TransactionListSampleTask transaction);
        ValueTask<SampleTask> GetSampleTaskByIdAsync(TransactionGetSampleTask transaction);
        ValueTask<SampleTask> AddSampleTaskAsync(TransactionAddSampleTask transaction);
        ValueTask<bool> UpdateSampleTaskTimerAsync(TransactionUpdateSampleTaskTimer transaction);

    }
}
