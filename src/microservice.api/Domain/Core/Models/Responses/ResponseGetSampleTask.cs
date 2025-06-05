using Domain.Core.Models.Entity;

namespace Domain.Core.Models.Responses
{
    public class ResponseGetSampleTask
    {

        public SampleTask SampleTaskInfo { get; init; }


        public ResponseGetSampleTask(SampleTask sample)
        {
            SampleTaskInfo = sample;
        }

    }
}
