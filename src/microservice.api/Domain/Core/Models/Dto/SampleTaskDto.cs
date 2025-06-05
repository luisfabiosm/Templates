using Domain.Core.Exceptions;
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

            validateSampleDto();

        }

        public SampleTaskDto(int id, int timer)
        {
            Id = id;
            _timer = timer;
        }

 


        public SampleTask MapSampleTask()
        {
            return new SampleTask(Name, IsTimerTrigged, TimerOnMilliseconds);
        }
 

        private void validateSampleDto()
        {
            BusinessException ex = new BusinessException();

            if (string.IsNullOrEmpty(_name))
                ex.AddDetails(new ErrorDetails("Uma Task precisa ter um nome definido"));

            if (_timer == 0 && _istrigged == true)
                ex.AddDetails(new ErrorDetails("Timer não pode ser 0"));

            if (ex.ErrorDetails.Count > 0) throw ex;
        }
    }
}
