using Microsoft.AspNetCore.Authentication;

namespace Adapters.Inbound.WebApi.Sample.Models
{
    public struct NewSampleTaskRequest
    {
        public int TimerInMilliseconds { get; set; }
        public string TaskName { get; set; }

        
        public NewSampleTaskRequest()
        {
            
        }

        public NewSampleTaskRequest(string name, int timer)
        {
            TimerInMilliseconds = timer;
            TaskName = name;        
        }
    }
}
