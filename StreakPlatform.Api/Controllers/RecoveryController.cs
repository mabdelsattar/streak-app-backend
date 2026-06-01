using Microsoft.AspNetCore.Mvc;
using StreakPlatform.Api.Auth;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Api.Controllers;

[ApiController]
[Route("api/streaks/{streakId:guid}")]
public class RecoveryController : ControllerBase
{
    private readonly IMissedDayRecoveryService _recovery;
    private readonly ICurrentUserAccessor _current;

    public RecoveryController(IMissedDayRecoveryService recovery, ICurrentUserAccessor current)
    {
        _recovery = recovery;
        _current = current;
    }

    /// <summary>What does the user owe for missed days?</summary>
    [HttpGet("debt")]
    public async Task<IActionResult> GetDebt(Guid streakId, CancellationToken ct) =>
        Ok(await _recovery.GetDebtAsync(_current.FirebaseUid, streakId, ct));

    /// <summary>Pay the recovery fee for all outstanding missed days at once.</summary>
    [HttpPost("pay-debt")]
    public async Task<IActionResult> PayDebt(Guid streakId, CancellationToken ct) =>
        Ok(await _recovery.PayDebtAsync(_current.FirebaseUid, streakId, ct));

    /// <summary>Soft-leave this streak (Participant.IsActive = false).</summary>
    [HttpPost("leave")]
    public async Task<IActionResult> Leave(Guid streakId, CancellationToken ct)
    {
        await _recovery.LeaveAsync(_current.FirebaseUid, streakId, ct);
        return NoContent();
    }
}
