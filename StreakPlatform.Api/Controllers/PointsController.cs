using Microsoft.AspNetCore.Mvc;
using StreakPlatform.Api.Auth;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Api.Controllers;

[ApiController]
[Route("api/users/me")]
public class PointsController : ControllerBase
{
    private readonly IPointsService _points;
    private readonly ICurrentUserAccessor _current;

    public PointsController(IPointsService points, ICurrentUserAccessor current)
    {
        _points = points;
        _current = current;
    }

    [HttpGet("points")]
    public async Task<IActionResult> Balance(CancellationToken ct) =>
        Ok(await _points.GetBalanceAsync(_current.FirebaseUid, ct));

    [HttpGet("points/transactions")]
    public async Task<IActionResult> Transactions([FromQuery] int take = 50, CancellationToken ct = default) =>
        Ok(await _points.GetTransactionsAsync(_current.FirebaseUid, take, ct));
}
