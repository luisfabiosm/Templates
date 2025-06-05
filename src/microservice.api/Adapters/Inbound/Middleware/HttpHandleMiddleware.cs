using Domain.Core.Base;
using Domain.Core.Exceptions;
using System.Net;
using System.Text.Json;

namespace Adapters.Inbound.Middleware
{
    public class HttpHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<HttpHandlingMiddleware> _logger;

        public HttpHandlingMiddleware(RequestDelegate next, ILogger<HttpHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro não tratado");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, response) = CreateErrorResponse(exception);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            await context.Response.WriteAsJsonAsync(response);
        }

        private static (int statusCode, BaseResponse<object> response) CreateErrorResponse(Exception exception)
        {
            return exception switch
            {
                ValidateException validationEx => (
                    StatusCodes.Status400BadRequest,
                    CreateBadRequestResponse(FormatValidationErrors(validationEx.ErrorDetails))
                ),
                BusinessException businessEx => (
                    StatusCodes.Status400BadRequest,
                    CreateBadRequestResponse(businessEx.Data)
                ),
                InternalException internalEx => (
                    StatusCodes.Status400BadRequest,
                    CreateBadRequestResponse(internalEx.Data)
                ),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    CreateServerErrorResponse(exception.Message)
                )
            };
        }

        private static BaseResponse<object> CreateBadRequestResponse(object errors)
        {
            return new BaseResponse<object>
            {
                Data = errors,
                ErrorCode = StatusCodes.Status400BadRequest,
                Message = "Requisição inválida"
            };
        }


        private static BaseResponse<object> CreateServerErrorResponse(string message)
        {
            return new BaseResponse<object>
            {
                Data = null,
                ErrorCode = StatusCodes.Status500InternalServerError,
                Message = message
            };
        }

        private static object FormatValidationErrors(IEnumerable<dynamic> errors)
        {
            return errors?.Select(error => new
            {
                campo = error.campo ?? "",
                mensagens = error.mensagens ?? ""
            }).ToList();
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<HttpHandlingMiddleware>();
        }
    }
}
