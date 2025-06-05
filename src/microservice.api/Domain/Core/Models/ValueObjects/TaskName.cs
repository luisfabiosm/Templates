using System;

namespace Domain.Core.Models.ValueObjects
{
    /// <summary>
    /// Value Object para nome da task seguindo Object Calisthenics
    /// Wrap primitives e validações de domínio
    /// </summary>
    public readonly record struct TaskName
    {
        private readonly string _value;

        public TaskName(string value)
        {
            ValidateName(value);
            _value = value.Trim();
        }

        public string Value => _value;

        private static void ValidateName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Nome da task não pode ser nulo ou vazio", nameof(value));

            if (value.Trim().Length < 3)
                throw new ArgumentException("Nome da task deve ter pelo menos 3 caracteres", nameof(value));

            if (value.Trim().Length > 100)
                throw new ArgumentException("Nome da task não pode ter mais de 100 caracteres", nameof(value));
        }

        public static implicit operator string(TaskName name) => name._value;
        public static implicit operator TaskName(string value) => new(value);

        public override string ToString() => _value;

    }
}