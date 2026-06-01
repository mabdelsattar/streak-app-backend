using Microsoft.AspNetCore.Mvc;
using StreakPlatform.Api.Auth;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Api.Controllers;

[ApiController]
[Route("api/streaks")]
public class StreaksController : ControllerBase
{
    private readonly IStreakService _streaks;
    private readonly ICurrentUserAccessor _current;

    public StreaksController(IStreakService streaks, ICurrentUserAccessor current)
    {
        _streaks = streaks;
        _current = current;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStreakRequest req, CancellationToken ct)
    {
        var dto = await _streaks.CreateAsync(_current.FirebaseUid, req, ct);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken ct) =>
        Ok(await _streaks.GetMyStreaksAsync(_current.FirebaseUid, ct));

    /// <summary>Discover public streaks I'm not in yet.</summary>
    [HttpGet("public")]
    public async Task<IActionResult> GetPublic(
        [FromQuery] int take = 20,
        [FromQuery] int skip = 0,
        [FromQuery] string? search = null,
        CancellationToken ct = default) =>
        Ok(await _streaks.GetPublicStreaksAsync(_current.FirebaseUid, take, skip, search, ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        Ok(await _streaks.GetDetailAsync(_current.FirebaseUid, id, ct));

    /// <summary>BRD §11.2 form — invite code in body.</summary>
    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> JoinById(Guid id, [FromBody] JoinStreakRequest req, CancellationToken ct)
    {
        var dto = await _streaks.JoinByInviteCodeAsync(_current.FirebaseUid, req.InviteCode, ct);
        if (dto.Id != id)
            return BadRequest(new { error = "Invite code does not match streak id." });
        return Ok(dto);
    }

    /// <summary>Practical join route — the user only needs the invite code.</summary>
    [HttpPost("join")]
    public async Task<IActionResult> JoinByCode([FromBody] JoinStreakRequest req, CancellationToken ct) =>
        Ok(await _streaks.JoinByInviteCodeAsync(_current.FirebaseUid, req.InviteCode, ct));

    /// <summary>Join a public streak directly (no invite code).</summary>
    [HttpPost("{id:guid}/join-public")]
    public async Task<IActionResult> JoinPublic(Guid id, CancellationToken ct) =>
        Ok(await _streaks.JoinPublicAsync(_current.FirebaseUid, id, ct));

    [HttpGet("{id:guid}/invite")]
    public async Task<IActionResult> GetInvite(Guid id, CancellationToken ct) =>
        Ok(await _streaks.GetInviteAsync(_current.FirebaseUid, id, ct));
}
