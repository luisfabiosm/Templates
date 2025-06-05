using Domain.Core.Base;
using Domain.Core.Models.Responses;

namespace Domain.UseCases.Sample.ListSampleTask
{
    public record TransactionListSampleTask : BaseTransaction<BaseReturn<ResponseListSampleTask>>
    {

        public TransactionListSampleTask()
        {
            Code = 3;
        }
    }
}
