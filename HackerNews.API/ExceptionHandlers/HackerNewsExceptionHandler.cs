using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace HackerNews.ExceptionHandlers;

public class HackerNewsExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not (HttpRequestException or TaskCanceledException or OperationCanceledException))
        {
            return false;
        }

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status502BadGateway,
            Title = "Upstream service unavailable",
            Detail = "The Hacker News API could not be reached. Please try again later.",
        };

        httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}