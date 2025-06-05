using Domain.Core.Base;
using Domain.Core.Models.Dto;
using Domain.Core.Models.Responses;

namespace Domain.UseCases.Sample.UpdateSampleTaskTimer
{
    public record TransactionUpdateSampleTaskTimer : BaseTransaction<BaseReturn<bool>>
    {
        private SampleTaskDto _sampleTaskDto;


        public int Id;
        public int TimerInMilliseconds;
  

        public TransactionUpdateSampleTaskTimer(int id, int timer)
        {
            Code = 4;
            Id = id;
            TimerInMilliseconds = timer;

            _sampleTaskDto = new SampleTaskDto(Id, TimerInMilliseconds);

        }

        public SampleTaskDto getSampleTaskDto()
        {
            return _sampleTaskDto;
        }

    }
}
