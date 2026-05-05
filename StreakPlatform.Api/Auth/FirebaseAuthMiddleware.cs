using FirebaseAdmin.Auth;

namespace StreakPlatform.Api.Auth;

public class FirebaseAuthMiddleware
{
    public const string FirebaseUidItemKey = "FirebaseUid";
    public const string FirebaseEmailItemKey = "FirebaseEmail";
    public const string FirebaseNameItemKey = "FirebaseName";

    private readonly RequestDelegate _next;
    private readonly ILogger<FirebaseAuthMiddleware> _log;

    public FirebaseAuthMiddleware(RequestDelegate next, ILogger<FirebaseAuthMiddleware> log)
    {
        _next = next;
        _log = log;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;

        if (path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        if (!path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        var auth = ctx.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await Deny(ctx, "Missing Bearer token.");
            return;
        }

        var token = auth["Bearer ".Length..].Trim();
        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token, ctx.RequestAborted);
            ctx.Items[FirebaseUidItemKey] = decoded.Uid;
            if (decoded.Claims.TryGetValue("email", out var email) && email is string s)
                ctx.Items[FirebaseEmailItemKey] = s;
            if (decoded.Claims.TryGetValue("name", out var name) && name is string n)
                ctx.Items[FirebaseNameItemKey] = n;
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Firebase token verification failed.");
            await Deny(ctx, "Invalid Firebase token.");
            return;
        }

        await _next(ctx);
    }

    private static Task Deny(HttpContext ctx, string reason)
    {
        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return ctx.Response.WriteAsJsonAsync(new { error = reason });
    }
}
