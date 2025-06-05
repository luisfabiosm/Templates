using System;

namespace Domain.Core.Models.ValueObjects
{
    /// <summary>
    /// Value Object para ID seguindo Object Calisthenics
    /// Wrap primitives - não usar int diretamente
    /// </summary>
    public readonly record struct SampleTaskId
    {
        private readonly int _value;

        public SampleTaskId(int value)
        {
            if (value <= 0)
                throw new ArgumentException("SampleTask ID deve ser maior que zero", nameof(value));

            _value = value;
        }

        public int Value => _value;

        public static implicit operator int(SampleTaskId id) => id._value;
        public static implicit operator SampleTaskId(int value) => new(value);

        public override string ToString() => _value.ToString();
    }
}