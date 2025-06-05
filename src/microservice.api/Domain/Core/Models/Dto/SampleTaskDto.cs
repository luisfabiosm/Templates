using Domain.Core.Models.Entity;

namespace Domain.Core.Models.Dto
{
    public record SampleTaskDto
    {
        private string _name;
        private bool _istrigged;
        private int _timer;

        public int Id { get; set; }
        public string Name { get => _name; }
        public bool IsTimerTrigged { get => _istrigged; }
        public int TimerOnMilliseconds { get => _timer; }

        public SampleTaskDto()
        {
            
        }
        public SampleTaskDto(string name, bool istrigged, int timer)
        {
            _name = name;
            _istrigged = istrigged;
            _timer = timer;

           

        }

        public SampleTaskDto(int id, int timer)
        {
            Id = id;
            _timer = timer;
        }

 


      
       
    }
}
