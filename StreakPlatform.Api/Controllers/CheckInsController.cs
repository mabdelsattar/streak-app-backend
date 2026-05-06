using Microsoft.AspNetCore.Mvc;
using StreakPlatform.Api.Auth;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Api.Controllers;

[ApiController]
[Route("api/streaks/{streakId:guid}")]
public class CheckInsController : ControllerBase
{
    private readonly ICheckInService _checkIns;
    private readonly ICurrentUserAccessor _current;

    public CheckInsController(ICheckInService checkIns, ICurrentUserAccessor current)
    {
        _checkIns = checkIns;
        _current = current;
    }

    [HttpPost("check-ins")]
    public async Task<IActionResult> Record(Guid streakId, [FromBody] CheckInRequest? req, CancellationToken ct) =>
        Ok(await _checkIns.RecordAsync(_current.FirebaseUid, streakId, req ?? new CheckInRequest(), ct));

    [HttpGet("check-ins/today")]
    public async Task<IActionResult> Today(Guid streakId, CancellationToken ct) =>
        Ok(await _checkIns.GetTodayStatusAsync(_current.FirebaseUid, streakId, ct));

    [HttpGet("status")]
    public async Task<IActionResult> Status(Guid streakId, CancellationToken ct) =>
        Ok(await _checkIns.GetStreakStatusAsync(_current.FirebaseUid, streakId, ct));

    [HttpGet("feed")]
    public async Task<IActionResult> Feed(Guid streakId, [FromQuery] int take = 20, [FromQuery] int skip = 0, CancellationToken ct = default) =>
        Ok(await _checkIns.GetFeedAsync(_current.FirebaseUid, streakId, take, skip, ct));
}
