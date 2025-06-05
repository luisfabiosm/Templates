using Domain.Core.Models.Entity;

namespace Domain.Core.Models.Responses
{
    public record ResponseNewSampleTask
    {

        public SampleTask SampleTaskinfo { get; init; }


        public ResponseNewSampleTask(SampleTask sample)
        {
            SampleTaskinfo = sample;
        }

    }
}
