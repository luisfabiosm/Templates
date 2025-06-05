using Domain.Core.Models.Entity;

namespace Domain.Core.Models.Responses
{
    public class ResponseListSampleTask
    {

        public List<SampleTask> Tasks { get; init; }


        public ResponseListSampleTask(List<SampleTask> tasks)
        {
            Tasks = tasks;
        }

    }
}
