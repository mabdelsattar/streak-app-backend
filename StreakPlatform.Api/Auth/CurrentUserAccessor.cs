using Microsoft.AspNetCore.Http;

namespace StreakPlatform.Api.Auth;

public interface ICurrentUserAccessor
{
    string FirebaseUid { get; }
    string? Email { get; }
    string? Name { get; }
}

public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _ctx;

    public CurrentUserAccessor(IHttpContextAccessor ctx) => _ctx = ctx;

    public string FirebaseUid =>
        _ctx.HttpContext?.Items[FirebaseAuthMiddleware.FirebaseUidItemKey] as string
            ?? throw new InvalidOperationException("No authenticated Firebase user on request.");

    public string? Email =>
        _ctx.HttpContext?.Items[FirebaseAuthMiddleware.FirebaseEmailItemKey] as string;

    public string? Name =>
        _ctx.HttpContext?.Items[FirebaseAuthMiddleware.FirebaseNameItemKey] as string;
}
