using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TransactionDisputePortal.Api.Integration;
using TransactionDisputePortal.Api.Models;

namespace TransactionDisputePortal.Api.Filters;

/// <summary>
/// Global exception filter that catches unhandled exceptions and returns standardized error responses
/// </summary>
public class GlobalExceptionFilter : IAsyncExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        var traceId = context.HttpContext.TraceIdentifier;
        var exception = context.Exception;

        _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);

        var errorResponse = new ErrorResponse
        {
            TraceId = traceId,
            Timestamp = DateTime.UtcNow,
            Details = exception.Message
        };

        // Determine status code and message based on exception type
        if (exception is ArgumentNullException or ArgumentException)
        {
            errorResponse.StatusCode = StatusCodes.Status400BadRequest;
            errorResponse.Message = "Invalid request data";
            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
        else if (exception is UnauthorizedAccessException)
        {
            errorResponse.StatusCode = StatusCodes.Status401Unauthorized;
            errorResponse.Message = "Unauthorized access";
            context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        }
        else if (exception is InvalidOperationException)
        {
            errorResponse.StatusCode = StatusCodes.Status409Conflict;
            errorResponse.Message = "Operation not allowed";
            context.HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        }
        else
        {
            errorResponse.StatusCode = StatusCodes.Status500InternalServerError;
            errorResponse.Message = "An unexpected error occurred";
            context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }

        context.Result = new JsonResult(errorResponse);
        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}
