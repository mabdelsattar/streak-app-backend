using Microsoft.AspNetCore.Mvc;
using StreakPlatform.Api.Auth;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ICurrentUserAccessor _current;

    public AuthController(IUserService users, ICurrentUserAccessor current)
    {
        _users = users;
        _current = current;
    }

    /// <summary>Idempotent — call once on every login to ensure the user row exists.</summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize([FromBody] InitializeUserRequest? req, CancellationToken ct)
    {
        var email = _current.Email ?? $"{_current.FirebaseUid}@unknown.local";
        var displayName = req?.DisplayName ?? _current.Name;
        var dto = await _users.InitializeAsync(_current.FirebaseUid, email, displayName, ct);
        return Ok(dto);
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct) =>
        Ok(await _users.GetProfileAsync(_current.FirebaseUid, ct));

    /// <summary>Logout is fully client-side with Firebase; this endpoint is kept for API parity.</summary>
    [HttpPost("logout")]
    public IActionResult Logout() => NoContent();
}
