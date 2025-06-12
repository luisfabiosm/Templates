using Domain.Core.Exceptions;

namespace Domain.Core.Models.Entity
{
   

    namespace Domain.Core.Models.Entity
    {
        public record SampleTask
        {
            public int Id { get; set; }
            public string Name { get; }
            public bool IsTimer { get; }
            public int TimerOnMiliseconds { get; }

            public SampleTask()
            {
                Name = string.Empty;
                IsTimer = false;
                TimerOnMiliseconds = 0;
            }

            public SampleTask(string name, bool isTimer, int timer)
            {
                ValidateTaskData(name, isTimer, timer);

                Name = name;
                IsTimer = isTimer;
                TimerOnMiliseconds = timer;
            }

            public SampleTask(int id, string name, bool isTimer, int timer)
            {
                ValidateTaskData(name, isTimer, timer);

                Id = id;
                Name = name;
                IsTimer = isTimer;
                TimerOnMiliseconds = timer;
            }

            public static SampleTask CreateNew(string name, bool isTimer, int timer)
                => new(name, isTimer, timer);

            public static SampleTask CreateFromRepository(int id, string name, bool isTimer, int timer)
                => new(id, name, isTimer, timer);

            public SampleTask WithTimer(int newTimer)
            {
                ValidateTimer(newTimer);
                return new SampleTask(Id, Name, IsTimer, newTimer);
            }

            public SampleTask WithName(string newName)
            {
                ValidateName(newName);
                return new SampleTask(Id, newName, IsTimer, TimerOnMiliseconds);
            }

            private static void ValidateTaskData(string name, bool isTimer, int timer)
            {
                ValidateName(name);

                if (isTimer)
                {
                    ValidateTimer(timer);
                }
            }

            private static void ValidateName(string name)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new BusinessException("Nome da task é obrigatório", 400);
                }
            }

            private static void ValidateTimer(int timer)
            {
                if (timer <= 0)
                {
                    throw new BusinessException("Timer deve ser maior que zero", 400);
                }
            }

            public bool HasValidTimer => TimerOnMiliseconds > 0;
            public bool IsActive => IsTimer && HasValidTimer;

            public override string ToString()
                => $"SampleTask(Id: {Id}, Name: {Name}, Timer: {TimerOnMiliseconds}ms, Active: {IsActive})";
        }
    }
}