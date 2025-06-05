using Domain.Core.Models.ValueObjects;
using System;

namespace Domain.Core.Models.Entity
{
    /// <summary>
    /// Entidade SampleTask seguindo Object Calisthenics
    /// - Máximo duas variáveis de instância por conceito
    /// - Sem getters/setters públicos
    /// - Imutável por design
    /// - Comportamentos de domínio encapsulados
    /// </summary>
    public sealed record SampleTask
    {
        public SampleTaskId Id { get; private init; }
        public TaskName Name { get; private init; }
        public TimerInMilliseconds Timer { get; private init; }
        public bool IsEnabled { get; private init; }
        public DateTime CreatedAt { get; private init; }
        public DateTime? UpdatedAt { get;  set; }

        // Construtor privado para garantir criação apenas através de factory methods
        private SampleTask(
            SampleTaskId id,
            TaskName name,
            TimerInMilliseconds timer,
            bool isEnabled,
            DateTime createdAt,
            DateTime? updatedAt = null)
        {
            Id = id;
            Name = name;
            Timer = timer;
            IsEnabled = isEnabled;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
        }

        // Factory method para criação de nova task
        public static SampleTask Create(TaskName name, TimerInMilliseconds timer)
        {
            return new SampleTask(
                id: default, // ID será definido pelo repositório
                name: name,
                timer: timer,
                isEnabled: true,
                createdAt: DateTime.UtcNow);
        }

        // Factory method para reconstrução a partir do banco
        public static SampleTask Reconstruct(
            SampleTaskId id,
            TaskName name,
            TimerInMilliseconds timer,
            bool isEnabled,
            DateTime createdAt,
            DateTime? updatedAt = null)
        {
            return new SampleTask(id, name, timer, isEnabled, createdAt, updatedAt);
        }

        // Comportamentos de domínio
        public SampleTask UpdateTimer(TimerInMilliseconds newTimer)
        {
            if (Timer == newTimer)
                return this;

            return this with
            {
                Timer = newTimer,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public SampleTask Enable()
        {
            if (IsEnabled)
                return this;

            return this with
            {
                IsEnabled = true,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public SampleTask Disable()
        {
            if (!IsEnabled)
                return this;

            return this with
            {
                IsEnabled = false,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public SampleTask Rename(TaskName newName)
        {
            if (Name == newName)
                return this;

            return this with
            {
                Name = newName,
                UpdatedAt = DateTime.UtcNow
            };
        }

        // Métodos de domínio para business rules
        public bool CanExecute() => IsEnabled;

        public bool IsTimerValid() => Timer.Value >= 500;

        public TimeSpan GetExecutionInterval() => Timer.ToTimeSpan();

        // Override ToString para melhor debugging
        public override string ToString() => $"SampleTask[{Id}] {Name} - {Timer} (Enabled: {IsEnabled})";

    }
}