using Domain.Core.Exceptions;
using Polly.Caching;
using System.Linq.Expressions;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;

namespace Domain.Core.Models.Entity
{
    public record SampleTask
    {

        public int Id { get; set; }
        public string Name { get;  }
        public bool IsTimer { get;  }
        public int TimerOnMiliseconds { get;  }

        public SampleTask()
        {
            
        }
        public SampleTask(string name, bool istrigged, int timer)
        {

            //this.Id = Guid.NewGuid();
            this.Name = name;
            this.TimerOnMiliseconds = timer;
            this.IsTimer = istrigged;

        }

    }
}
