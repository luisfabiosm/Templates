using System.Collections.Generic;
using System.Linq;

namespace Domain.Core.ResultPattern
{
    /// <summary>
    /// Result específico para validações seguindo SRP
    /// </summary>
    public readonly record struct BSValidationResult
    {
        private readonly IReadOnlyList<BSValidationError> _errors;
        private readonly bool _isValid;

        private BSValidationResult(IReadOnlyList<BSValidationError> errors, bool isValid)
        {
            _errors = errors ?? Array.Empty<BSValidationError>();
            _isValid = isValid;
        }

        public bool IsValid => _isValid;
        public bool IsInvalid => !_isValid;
        public IReadOnlyList<BSValidationError> Errors => _errors;

        public static BSValidationResult Valid() => new(Array.Empty<BSValidationError>(), true);
        public static BSValidationResult Invalid(params BSValidationError[] errors) => new(errors, false);
        public static BSValidationResult Invalid(IEnumerable<BSValidationError> errors) => new(errors.ToArray(), false);

        public BSValidationResult Combine(BSValidationResult other)
        {
            if (IsValid && other.IsValid)
                return Valid();

            var combinedErrors = _errors.Concat(other._errors).ToArray();
            return Invalid(combinedErrors);
        }
    }

    public readonly record struct BSValidationError(string PropertyName, string Message)
    {
        public static BSValidationError Create(string propertyName, string message) => new(propertyName, message);
        public override string ToString() => $"{PropertyName}: {Message}";
    }
}