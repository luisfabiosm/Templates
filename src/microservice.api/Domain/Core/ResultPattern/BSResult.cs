using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Core.ResultPattern
{
    /// <summary>
    /// Result pattern para evitar exceptions seguindo princípios de performance
    /// Implementação thread-safe e com baixo uso de memória
    /// </summary>
    public readonly record struct BSResult<T>
    {
        private readonly T _value;
        private readonly BSError _error;
        private readonly bool _isSuccess;

        private BSResult(T value, BSError error, bool isSuccess)
        {
            _value = value;
            _error = error;
            _isSuccess = isSuccess;
        }

        public bool IsSuccess => _isSuccess;
        public bool IsFailure => !_isSuccess;

        public T Value => IsSuccess ? _value : throw new InvalidOperationException("Não é possível acessar Value em um Result com falha");
        public BSError Error => IsFailure ? _error : throw new InvalidOperationException("Não é possível acessar Error em um Result com sucesso");

        public static BSResult<T> Success(T value) => new(value, default, true);
        public static BSResult<T> Failure(BSError error) => new(default, error, false);

        public static implicit operator BSResult<T>(T value) => Success(value);
        public static implicit operator BSResult<T>(BSError error) => Failure(error);

        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<BSError, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(_value) : onFailure(_error);
        }

        public BSResult<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            return IsSuccess ? BSResult<TResult>.Success(mapper(_value)) : BSResult<TResult>.Failure(_error);
        }

        public BSResult<TResult> Bind<TResult>(Func<T, BSResult<TResult>> binder)
        {
            return IsSuccess ? binder(_value) : BSResult<TResult>.Failure(_error);
        }
    }

    /// <summary>
    /// Result para operações sem retorno
    /// </summary>
    public readonly record struct BSResult
    {
        private readonly BSError _error;
        private readonly bool _isSuccess;

        private BSResult(BSError error, bool isSuccess)
        {
            _error = error;
            _isSuccess = isSuccess;
        }

        public bool IsSuccess => _isSuccess;
        public bool IsFailure => !_isSuccess;
        public BSError Error => IsFailure ? _error : throw new InvalidOperationException("Não é possível acessar Error em um Result com sucesso");

        public static BSResult Success() => new(default, true);
        public static BSResult Failure(BSError error) => new(error, false);

        public static implicit operator BSResult(BSError error) => Failure(error);

        public TResult Match<TResult>(Func<TResult> onSuccess, Func<BSError, TResult> onFailure)
        {
            return IsSuccess ? onSuccess() : onFailure(_error);
        }
    }
}


