using Domain.Core.Base;
using Domain.Core.Enums;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Domain.Core.Exceptions
{

    public class InternalException : Exception
    {

        public int ErrorCode { get; } = 1;

        public List<object> ErrorDetails = new List<object>();
        public InternalException(string message)
            : base(message)
        {
            ErrorDetails.Add(new BaseError(ErrorCode, message, EnumErrorType.System));
            ErrorCode = -1;
        }


        public InternalException(string message, int errorCode, object details)
            : base(message)
        {
            ErrorDetails.Add(new BaseError(errorCode, message, EnumErrorType.System));
            ErrorCode = errorCode;
        }

        public InternalException(string message, int errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorDetails.Add(new BaseError(ErrorCode, message, EnumErrorType.System));
            ErrorCode = errorCode;
        }

    }
}