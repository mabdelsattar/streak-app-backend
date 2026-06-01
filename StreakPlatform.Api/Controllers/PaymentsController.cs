using Microsoft.AspNetCore.Mvc;
using StreakPlatform.Api.Auth;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _payments;
    private readonly ICurrentUserAccessor _current;

    public PaymentsController(IPaymentService payments, ICurrentUserAccessor current)
    {
        _payments = payments;
        _current = current;
    }

    /// <summary>List the available point packs.</summary>
    [HttpGet("packs")]
    public IActionResult Packs() => Ok(_payments.GetPacks());

    /// <summary>Buy a points pack. Mock provider always succeeds.</summary>
    [HttpPost("purchase")]
    public async Task<IActionResult> Purchase([FromBody] PurchaseRequest req, CancellationToken ct) =>
        Ok(await _payments.PurchaseAsync(_current.FirebaseUid, req.PackId, ct));

    /// <summary>List the user's past purchases (receipts).</summary>
    [HttpGet("history")]
    public async Task<IActionResult> History([FromQuery] int take = 20, CancellationToken ct = default) =>
        Ok(await _payments.GetHistoryAsync(_current.FirebaseUid, take, ct));
}
