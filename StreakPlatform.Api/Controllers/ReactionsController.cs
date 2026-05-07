using Microsoft.AspNetCore.Mvc;
using StreakPlatform.Api.Auth;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Api.Controllers;

[ApiController]
[Route("api/check-ins/{checkInId:guid}/reactions")]
public class ReactionsController : ControllerBase
{
    private readonly IReactionService _reactions;
    private readonly ICurrentUserAccessor _current;

    public ReactionsController(IReactionService reactions, ICurrentUserAccessor current)
    {
        _reactions = reactions;
        _current = current;
    }

    /// <summary>Like or dislike a check-in. Idempotent on same type; flips on different type.</summary>
    [HttpPost]
    public async Task<IActionResult> React(Guid checkInId, [FromBody] ReactCheckInRequest req, CancellationToken ct) =>
        Ok(await _reactions.ReactAsync(_current.FirebaseUid, checkInId, req.Type, ct));

    /// <summary>Remove your reaction (refunds points).</summary>
    [HttpDelete("me")]
    public async Task<IActionResult> Remove(Guid checkInId, CancellationToken ct)
    {
        await _reactions.RemoveAsync(_current.FirebaseUid, checkInId, ct);
        return NoContent();
    }
}
