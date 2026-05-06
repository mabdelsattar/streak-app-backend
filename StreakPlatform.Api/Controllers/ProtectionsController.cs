using Microsoft.AspNetCore.Mvc;
using StreakPlatform.Api.Auth;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Api.Controllers;

[ApiController]
public class ProtectionsController : ControllerBase
{
    private readonly IStreakProtectionService _service;
    private readonly ICurrentUserAccessor _current;

    public ProtectionsController(IStreakProtectionService service, ICurrentUserAccessor current)
    {
        _service = service;
        _current = current;
    }

    /// <summary>Mode A — pre-activate protection on a streak. Idempotent.</summary>
    [HttpPost("api/streaks/{streakId:guid}/protect")]
    public async Task<IActionResult> Activate(Guid streakId, CancellationToken ct) =>
        Ok(await _service.ActivateAsync(_current.FirebaseUid, streakId, ct));

    /// <summary>Cancel pending protection on a streak.</summary>
    [HttpDelete("api/streaks/{streakId:guid}/protect")]
    public async Task<IActionResult> Cancel(Guid streakId, CancellationToken ct)
    {
        await _service.CancelAsync(_current.FirebaseUid, streakId, ct);
        return NoContent();
    }

    /// <summary>Mode C — restore a streak broken yesterday (within 24h).</summary>
    [HttpPost("api/streaks/{streakId:guid}/restore")]
    public async Task<IActionResult> Restore(Guid streakId, CancellationToken ct) =>
        Ok(await _service.RestoreAsync(_current.FirebaseUid, streakId, ct));

    /// <summary>List my pending protections across all streaks.</summary>
    [HttpGet("api/users/me/protections")]
    public async Task<IActionResult> Mine(CancellationToken ct) =>
        Ok(await _service.GetMyPendingAsync(_current.FirebaseUid, ct));
}
