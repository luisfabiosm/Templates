using Domain.Core.Enums;
using Domain.Core.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Domain.Core.Common.ResultPattern;


public record BaseReturn<T>
{

    public bool Success { get; }

    public string Message { get; }

    public int ErrorCode { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; }

    public string? CorrelationId { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? ErrorDetails { get; }

    [JsonIgnore]
    public DateTime CreatedAt { get; }

    private BaseReturn(
        bool success,
        string message,
        int errorCode,
        T? data,
        string? correlationId,
        object? errorDetails)
    {
        Success = success;
        Message = message;
        ErrorCode = errorCode;
        Data = data;
        CorrelationId = correlationId;
        ErrorDetails = errorDetails;
        CreatedAt = DateTime.UtcNow;
    }


    [MemberNotNullWhen(true, nameof(Data))]
    [JsonIgnore]
    public bool IsSuccess => Success;

    [MemberNotNullWhen(true, nameof(ErrorDetails))]
    [JsonIgnore]
    public bool IsFailure => !Success;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BaseReturn<T> FromSuccess(
        T data,
        string message = "Operation completed successfully",
        string? correlationId = null)
    {
        return new BaseReturn<T>(
            success: true,
            message: message,
            errorCode: 0,
            data: data,
            correlationId: correlationId,
            errorDetails: null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BaseReturn<T> FromError(
        string message,
        int errorCode = -1,
        string? correlationId = null,
        object? errorDetails = null)
    {
        return new BaseReturn<T>(
            success: false,
            message: message,
            errorCode: errorCode,
            data: default,
            correlationId: correlationId,
            errorDetails: errorDetails);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BaseReturn<T> FromException(
        Exception exception,
        string? correlationId = null,
        bool includeDetails = false)
    {
        return exception switch
        {
            BusinessException businessEx => new BaseReturn<T>(
                success: false,
                message: businessEx.Message,
                errorCode: businessEx.ErrorCode,
                data: default,
                correlationId: correlationId,
                errorDetails: includeDetails ? businessEx.ErrorDetails : null),

            ValidateException validateEx => new BaseReturn<T>(
                success: false,
                message: validateEx.Message,
                errorCode: validateEx.ErrorCode,
                data: default,
                correlationId: correlationId,
                errorDetails: validateEx.ErrorDetails),

            InternalException internalEx => new BaseReturn<T>(
                success: false,
                message: internalEx.Message,
                errorCode: internalEx.ErrorCode,
                data: default,
                correlationId: correlationId,
                errorDetails: includeDetails ? internalEx.ErrorDetails : null),

            _ => new BaseReturn<T>(
                success: false,
                message: exception.Message,
                errorCode: -1,
                data: default,
                correlationId: correlationId,
                errorDetails: includeDetails ? exception.ToString() : null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BaseReturn<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (!Success)
        {
            return BaseReturn<TNew>.FromError(Message, ErrorCode, CorrelationId, ErrorDetails);
        }

        try
        {
            var newData = mapper(Data!);
            return BaseReturn<TNew>.FromSuccess(newData, Message, CorrelationId);
        }
        catch (Exception ex)
        {
            return BaseReturn<TNew>.FromException(ex, CorrelationId);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<BaseReturn<TNew>> MapAsync<TNew>(Func<T, ValueTask<TNew>> mapper)
    {
        if (!Success)
        {
            return BaseReturn<TNew>.FromError(Message, ErrorCode, CorrelationId, ErrorDetails);
        }

        try
        {
            var newData = await mapper(Data!);
            return BaseReturn<TNew>.FromSuccess(newData, Message, CorrelationId);
        }
        catch (Exception ex)
        {
            return BaseReturn<TNew>.FromException(ex, CorrelationId);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BaseReturn<T> OnSuccess(Action<T> action)
    {
        if (Success && Data is not null)
        {
            action(Data);
        }
        return this;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BaseReturn<T> OnError(Action<string, int> action)
    {
        if (!Success)
        {
            action(Message, ErrorCode);
        }
        return this;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BaseReturn<TNew> Bind<TNew>(Func<T, BaseReturn<TNew>> binder)
    {
        if (!Success)
        {
            return BaseReturn<TNew>.FromError(Message, ErrorCode, CorrelationId, ErrorDetails);
        }

        try
        {
            return binder(Data!);
        }
        catch (Exception ex)
        {
            return BaseReturn<TNew>.FromException(ex, CorrelationId);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public async ValueTask<BaseReturn<TNew>> BindAsync<TNew>(Func<T, ValueTask<BaseReturn<TNew>>> binder)
    {
        if (!Success)
        {
            return BaseReturn<TNew>.FromError(Message, ErrorCode, CorrelationId, ErrorDetails);
        }

        try
        {
            return await binder(Data!);
        }
        catch (Exception ex)
        {
            return BaseReturn<TNew>.FromException(ex, CorrelationId);
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault(T defaultValue = default!)
    {
        return Success ? Data! : defaultValue;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueOrDefault(Func<T> defaultFactory)
    {
        return Success ? Data! : defaultFactory();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfError()
    {
        if (!Success)
        {
            throw ErrorCode switch
            {
                400 => new BusinessException(Message, ErrorCode, ErrorDetails),
                -1 => new ValidateException(Message, ErrorCode, ErrorDetails),
                _ => new InternalException(Message, ErrorCode, ErrorDetails)
            };
        }
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator BaseReturn<T>(T value)
    {
        return FromSuccess(value);
    }

   
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator BaseReturn<T>(Exception exception)
    {
        return FromException(exception);
    }


    public override string ToString()
    {
        return Success
            ? $"Success: {Message} [Data: {Data}]"
            : $"Error: {Message} [Code: {ErrorCode}]";
    }
}


public readonly record struct BaseReturn
{
    public bool Success { get; }
    public string Message { get; }
    public int ErrorCode { get; }
    public string? CorrelationId { get; }
    public object? ErrorDetails { get; }

    [JsonIgnore]
    public DateTime CreatedAt { get; }

    private BaseReturn(
        bool success,
        string message,
        int errorCode,
        string? correlationId,
        object? errorDetails)
    {
        Success = success;
        Message = message;
        ErrorCode = errorCode;
        CorrelationId = correlationId;
        ErrorDetails = errorDetails;
        CreatedAt = DateTime.UtcNow;
    }

    [JsonIgnore]
    public bool IsSuccess => Success;

    [JsonIgnore]
    public bool IsFailure => !Success;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BaseReturn FromSuccess(
        string message = "Operation completed successfully",
        string? correlationId = null)
    {
        return new BaseReturn(true, message, 0, correlationId, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BaseReturn FromError(
        string message,
        int errorCode = -1,
        string? correlationId = null,
        object? errorDetails = null)
    {
        return new BaseReturn(false, message, errorCode, correlationId, errorDetails);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static BaseReturn FromException(
        Exception exception,
        string? correlationId = null,
        bool includeDetails = false)
    {
        return exception switch
        {
            BusinessException businessEx => new BaseReturn(
                false, businessEx.Message, businessEx.ErrorCode, correlationId,
                includeDetails ? businessEx.ErrorDetails : null),
            ValidateException validateEx => new BaseReturn(
                false, validateEx.Message, validateEx.ErrorCode, correlationId,
                validateEx.ErrorDetails),
            InternalException internalEx => new BaseReturn(
                false, internalEx.Message, internalEx.ErrorCode, correlationId,
                includeDetails ? internalEx.ErrorDetails : null),
            _ => new BaseReturn(
                false, exception.Message, -1, correlationId,
                includeDetails ? exception.ToString() : null)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ThrowIfError()
    {
        if (!Success)
        {
            throw ErrorCode switch
            {
                400 => new BusinessException(Message, ErrorCode, ErrorDetails),
                -1 => new ValidateException(Message, ErrorCode, ErrorDetails),
                _ => new InternalException(Message, ErrorCode, ErrorDetails)
            };
        }
    }

    public override string ToString()
    {
        return Success
            ? $"Success: {Message}"
            : $"Error: {Message} [Code: {ErrorCode}]";
    }
}