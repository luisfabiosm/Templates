using System;

namespace Domain.Core.Models.ValueObjects
{
    /// <summary>
    /// Value Object para timer seguindo Object Calisthenics
    /// Wrap primitives com validações de domínio
    /// </summary>
    public readonly record struct TimerInMilliseconds
    {
        private readonly int _value;
        private const int MinimumTimer = 500;
        private const int MaximumTimer = 86400000; // 24 horas

        public TimerInMilliseconds(int value)
        {
            ValidateTimer(value);
            _value = value;
        }

        public int Value => _value;

        private static void ValidateTimer(int value)
        {
            if (value < MinimumTimer)
                throw new ArgumentException($"Timer deve ser pelo menos {MinimumTimer}ms", nameof(value));

            if (value > MaximumTimer)
                throw new ArgumentException($"Timer não pode exceder {MaximumTimer}ms", nameof(value));
        }

        public TimeSpan ToTimeSpan() => TimeSpan.FromMilliseconds(_value);

        public static implicit operator int(TimerInMilliseconds timer) => timer._value;
        public static implicit operator TimerInMilliseconds(int value) => new(value);

        public override string ToString() => $"{_value}ms";
    }
}