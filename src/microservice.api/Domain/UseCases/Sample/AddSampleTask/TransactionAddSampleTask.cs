using Domain.Core.Base;
using Domain.Core.Models.Dto;
using Domain.Core.Models.Responses;

namespace Domain.UseCases.Sample.AddSampleTask
{
    public record TransactionAddSampleTask : BaseTransaction<BaseReturn<ResponseNewSampleTask>>
    {
        private SampleTaskDto _sampleTaskDto;


        public string Name { get; private set; } = string.Empty;

        public int TimerInMilliseconds { get; init; }

        public bool IsEnable { get; init; }


        public SampleTaskDto getSampleTaskDto()
        {
            return _sampleTaskDto;
        }

      

        public TransactionAddSampleTask(string name, int timer, bool isenable)
        {
            Code = 1;
            Name = name;
            TimerInMilliseconds = timer;
            IsEnable = isenable;


            _sampleTaskDto = new SampleTaskDto(Name, IsEnable, TimerInMilliseconds );


        }



    }
}
