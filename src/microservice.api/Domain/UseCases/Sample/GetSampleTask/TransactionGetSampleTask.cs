using Domain.Core.Base;
using Domain.Core.Models.Responses;

namespace Domain.UseCases.Sample.GetSampleTask
{
    public record TransactionGetSampleTask : BaseTransaction<BaseReturn<ResponseGetSampleTask>>
    {
        public int Id { get; set; } = 0;


        public TransactionGetSampleTask(int id)
        {
           Code = 2;
           Id = id;
        }
    }
}
