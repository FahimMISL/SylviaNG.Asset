using SylviaNG.Assets.Application.Common.Exceptions;
using RMS.Application.Interfaces;
using System.Text.Json;

namespace SylviaNG.Assets.Middlewares
{
    public class GlobalExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

        public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IAuditLogger auditLogger, ICurrentUserService currentUser)
        {
            try
            {
                await _next(context);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found.");
                await HandleExceptionAsync(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "Conflict.");
                await HandleExceptionAsync(context, StatusCodes.Status409Conflict, ex.Message);
            }
            catch (ForbiddenException ex)
            {
                _logger.LogWarning(ex, "Forbidden.");
                // Feature 10 (Section 10): one hook covers every 403 across the whole app, existing and
                // new, without touching individual handlers. Best-effort - a failure here must never
                // hide the real 403 response from the caller.
                try
                {
                    await auditLogger.LogAsync(
                        "UnauthorizedAccessAttempt", "Endpoint", Guid.Empty,
                        $"Path={context.Request.Method} {context.Request.Path}; Role={currentUser.Role}; Reason={ex.Message}",
                        context.RequestAborted);
                }
                catch (Exception auditEx)
                {
                    _logger.LogError(auditEx, "Failed to audit-log an UnauthorizedAccessAttempt.");
                }
                await HandleExceptionAsync(context, StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed.");
                var errors = ex.Errors.Select(e => e.ErrorMessage).ToList();

                var response = new
                {
                    hasError = true,
                    decentMessage = "Validation failed.",
                    errorDetails = errors,
                    content = (object?)null
                };

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred.");
                await HandleExceptionAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please contact support.");
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var response = new
            {
                hasError = true,
                decentMessage = message,
                errorDetails = (string?)null,
                content = (object?)null
            };

            var json = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(json);
        }
    }
}
