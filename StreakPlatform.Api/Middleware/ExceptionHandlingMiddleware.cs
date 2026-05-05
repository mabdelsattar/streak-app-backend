using Microsoft.AspNetCore.Mvc;
using StreakPlatform.Application.Common;

namespace StreakPlatform.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (NotFoundException ex)
        {
            await Write(ctx, StatusCodes.Status404NotFound, "not_found", "Not Found", ex.Message);
        }
        catch (ConflictException ex)
        {
            await Write(ctx, StatusCodes.Status409Conflict, "conflict", "Conflict", ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await Write(ctx, StatusCodes.Status403Forbidden, "forbidden", "Forbidden", ex.Message);
        }
        catch (ValidationException ex)
        {
            await Write(ctx, StatusCodes.Status400BadRequest, "validation_error", "Bad Request", ex.Message);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception");
            await Write(ctx, StatusCodes.Status500InternalServerError, "server_error", "Server Error", "An unexpected error occurred.");
        }
    }

    private static Task Write(HttpContext ctx, int status, string code, string title, string detail)
    {
        ctx.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = code
        };
        problem.Extensions["code"] = code;
        return ctx.Response.WriteAsJsonAsync(problem);
    }
}
