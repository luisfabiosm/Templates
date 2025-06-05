using MongoDB.Bson.Serialization.Conventions;
using System;

namespace Domain.Core.Exceptions
{

    public class ValidateException : Exception
    {
        public int ErrorCode { get; internal set; } = -1;

        public List<ErrorDetails> ErrorDetails { get; private set; }

        public ValidateException()
        {
            
        }
        public void AddDetails(ErrorDetails details)
        {
            this.ErrorDetails.Add(details);
        }

        public ValidateException(string message)
            : base(message)
        {
            this.ErrorDetails = new List<ErrorDetails>();
        }

        public ValidateException(string message, int errorCode, object details)
           : base(message)
        {
            this.ErrorCode = errorCode == -1 ? 400 : errorCode;
            ErrorDetails = (List<ErrorDetails>)details;
        }

    }
}