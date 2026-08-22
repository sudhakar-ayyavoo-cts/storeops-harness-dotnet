using System.Text.Json;
using StoreOps.Application.Common;

namespace StoreOps.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (AppError appError)
        {
            context.Response.StatusCode = appError.StatusCode;
            context.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(new
            {
                code = appError.Code,
                message = appError.Message,
            });

            await context.Response.WriteAsync(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception — harness violation: non-AppError reached middleware.");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new
                {
                    code = "INTERNAL_ERROR",
                    message = "An unexpected error occurred.",
                }));
        }
    }
}
