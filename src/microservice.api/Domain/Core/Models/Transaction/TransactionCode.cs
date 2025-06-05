namespace Domain.Core.Models.Transaction
{
    /// <summary>
    /// Value Object para Transaction Code seguindo Object Calisthenics
    /// Wrap primitives - não usar int diretamente
    /// </summary>
    public readonly record struct TransactionCode
    {
        private readonly int _value;

        public TransactionCode(int value)
        {
            ValidateCode(value);
            _value = value;
        }

        public int Value => _value;

        private static void ValidateCode(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentException("Transaction code deve ser maior que zero", nameof(value));
            }
        }

        public static implicit operator int(TransactionCode code) => code._value;
        public static implicit operator TransactionCode(int value) => new(value);

        public override string ToString() => _value.ToString();
    }
}
