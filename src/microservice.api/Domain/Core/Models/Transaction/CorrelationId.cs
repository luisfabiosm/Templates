namespace Domain.Core.Models.Transaction
{
    /// <summary>
    /// Value Object para Correlation ID seguindo Object Calisthenics
    /// Wrap primitives - não usar string diretamente
    /// </summary>
    public readonly record struct CorrelationId
    {
        private readonly string _value;

        public CorrelationId(string value)
        {
            ValidateCorrelationId(value);
            _value = value;
        }

        public CorrelationId() : this(Guid.NewGuid().ToString()) { }

        public string Value => _value;

        private static void ValidateCorrelationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Correlation ID não pode ser nulo ou vazio", nameof(value));
            }
        }

        public static implicit operator string(CorrelationId correlationId) => correlationId._value;
        public static implicit operator CorrelationId(string value) => new(value);

        public override string ToString() => _value;
    }
}
