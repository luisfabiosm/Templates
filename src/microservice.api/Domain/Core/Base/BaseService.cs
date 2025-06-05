using Domain.Core.Exceptions;
using Domain.Core.Interfaces.Outbound;
using System;

namespace Domain.Core.Base
{
    public class BaseService
    {

        protected readonly ILoggingAdapter _loggingAdapter;

        public BaseService(IServiceProvider serviceProvider)
        {
            _loggingAdapter = serviceProvider.GetRequiredService<ILoggingAdapter>();
        }


        protected Exception HandleException(string methodName, Exception exception)
        {
            _loggingAdapter.LogError(methodName, exception);
            return (IsKnownException(exception) ? exception : UnknownException(exception));
        }

        private static bool IsKnownException(Exception exception)
        {
            return exception is BusinessException or InternalException or ValidateException;
        }

        private static Exception UnknownException(Exception exception)
        {
            return new InternalException(
                exception.Message ?? "Erro interno não esperado",
                1,
                exception);
        }

     

    }
}
