using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace StoreAnalytics;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return false;

        logger.LogError(exception, "Request {TraceId} failed.", httpContext.TraceIdentifier);
        int status = exception is NpgsqlException { IsTransient: true } or TimeoutException
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status500InternalServerError;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = status == StatusCodes.Status503ServiceUnavailable
                ? "Service temporarily unavailable."
                : "An unexpected error occurred.",
            Extensions = { ["traceId"] = httpContext.TraceIdentifier }
        }, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
