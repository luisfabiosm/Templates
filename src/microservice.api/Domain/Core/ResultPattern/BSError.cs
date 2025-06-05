using System;
using System.Collections.Generic;
using System.Linq;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Core.ResultPattern
{
    /// <summary>
    /// Value Object para representar erros seguindo Object Calisthenics
    /// Imutável e thread-safe
    /// </summary>
    public readonly record struct BSError
    {
        public ErrorCode Code { get; }
        public ErrorMessage Message { get; }
        public ErrorType Type { get; }

        public BSError(string code, string message, ErrorType type = ErrorType.Business)
        {
            Code = new ErrorCode(code);
            Message = new ErrorMessage(message);
            Type = type;
        }

        public BSError(int code, string message, ErrorType type = ErrorType.Business)
        {
            Code = new ErrorCode(code.ToString());
            Message = new ErrorMessage(message);
            Type = type;
        }

        public static BSError Business(string message) => new("BUSINESS_ERROR", message, ErrorType.Business);
        public static BSError Validation(string message) => new("VALIDATION_ERROR", message, ErrorType.Validation);
        public static BSError Internal(string message) => new("INTERNAL_ERROR", message, ErrorType.Internal);
        public static BSError NotFound(string message) => new("NOT_FOUND", message, ErrorType.NotFound);

        public override string ToString() => $"[{Code}] {Message}";
    }

    public readonly record struct ErrorCode(string Value)
    {
        public static implicit operator string(ErrorCode code) => code.Value;
        public static implicit operator ErrorCode(string value) => new(value);
    }

    public readonly record struct ErrorMessage(string Value)
    {
        public static implicit operator string(ErrorMessage message) => message.Value;
        public static implicit operator ErrorMessage(string value) => new(value);
    }

    public enum ErrorType
    {
        Business,
        Validation,
        Internal,
        NotFound
    }
}