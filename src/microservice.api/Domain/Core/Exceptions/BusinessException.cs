using Domain.Core.Base;
using System;

namespace Domain.Core.Exceptions
{
  
    public class BusinessException : Exception
    {

        public int ErrorCode { get; } = 400;

        public List<object> ErrorDetails = new List<object>();

        public BusinessException()
        {
            
        }
        public BusinessException(string message)
            : base(message)
        {
            ErrorCode = 400;
            ErrorDetails.Add(new BaseError(ErrorCode, message, Enums.EnumErrorType.Business));
        }


        public BusinessException(string message, int errorCode)
            : base(message)
        {
            ErrorCode = errorCode;
            ErrorDetails.Add(new BaseError(ErrorCode, message, Enums.EnumErrorType.Business));
        }


        public BusinessException(string message, int errorCode, object details)
            : base(message)
        {
            ErrorCode = errorCode;
            ErrorDetails.Add(new BaseError(ErrorCode, message, Enums.EnumErrorType.Business));

        }

        public void AddDetails(ErrorDetails details)
        {
            this.ErrorDetails.Add(details);
        }

    }
}