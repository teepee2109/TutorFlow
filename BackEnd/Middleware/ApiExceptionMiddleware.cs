using BackEnd.Dtos.Api;
using BackEnd.Services.Common;

namespace BackEnd.Middleware;

/// <summary>
/// Keeps unhandled server and database details out of API responses while returning
/// the same JSON envelope used by the controllers.
/// </summary>
public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled API exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
            {
                throw;
            }

            var mapped = SqlExceptionMapper.Map(exception);
            context.Response.Clear();
            context.Response.StatusCode = mapped.HasValue
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status500InternalServerError;

            var message = mapped?.Message ?? "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau.";
            await context.Response.WriteAsJsonAsync(ApiResponse.Fail(message));
        }
    }
}
