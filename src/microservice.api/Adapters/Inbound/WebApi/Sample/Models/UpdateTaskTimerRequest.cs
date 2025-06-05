namespace Adapters.Inbound.WebApi.Sample.Models
{
    public struct UpdateTaskTimerRequest
    {
        public int Id { get; set; }

        public int TimerInMilliseconds { get; set; }

        public UpdateTaskTimerRequest(int id, int timer)
        {
            TimerInMilliseconds = timer;
            Id = id;
        }

    }
}
