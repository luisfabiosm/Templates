using Domain.Core.Models.Dto;
using Domain.Core.Models.Entity;
using Domain.UseCases.Sample.AddSampleTask;
using Domain.UseCases.Sample.GetSampleTask;
using Domain.UseCases.Sample.ListSampleTask;
using Domain.UseCases.Sample.UpdateSampleTaskTimer;

namespace Domain.Core.Interfaces.Outbound
{
    public interface INoSQLSampleRepository
    {


        ValueTask<(List<SampleTask>, Exception exception)> ListAllSampleTaskAsync(TransactionListSampleTask transaction);
        ValueTask<(SampleTask, Exception exception)> GetSampleTaskByIdAsync(TransactionGetSampleTask transaction);
        ValueTask<(SampleTask, Exception exception)> AddSampleTaskAsync(TransactionAddSampleTask transaction);
        ValueTask<(bool, Exception exception)> UpdateSampleTaskTimerAsync(TransactionUpdateSampleTaskTimer transaction);

    }
}
