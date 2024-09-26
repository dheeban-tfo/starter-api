﻿using System.Net;
using System.Text.Json;

namespace starterapi.Middleware
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger, IHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {ExceptionType}", ex.GetType().Name);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var code = HttpStatusCode.InternalServerError;
            var result = string.Empty;

            switch (exception)
            {
                case UserNotAuthenticatedException:
                    code = HttpStatusCode.Unauthorized;
                    break;
                case MissingTenantIdException:
                case MissingModuleOrPermissionAttributeException:
                case InsufficientPermissionsException:
                    code = HttpStatusCode.Forbidden;
                    break;
                // Add other custom exceptions here
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)code;

            var errorResponse = new 
            {
                error = exception.Message,
                exceptionType = exception.GetType().Name,
                stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
            };

            result = JsonSerializer.Serialize(errorResponse);

            _logger.LogInformation("Returning error response: {StatusCode} {ExceptionType} {ErrorMessage}", 
                (int)code, exception.GetType().Name, exception.Message);

            return context.Response.WriteAsync(result);
        }
    }
}