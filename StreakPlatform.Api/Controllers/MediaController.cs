using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StreakPlatform.Api.Auth;
using StreakPlatform.Application.Common;
using StreakPlatform.Application.DTOs;
using StreakPlatform.Application.Interfaces;

namespace StreakPlatform.Api.Controllers;

[ApiController]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IMediaStorage _storage;
    private readonly IUserRepository _users;
    private readonly ICurrentUserAccessor _current;
    private readonly AppOptions _options;

    public MediaController(
        IMediaStorage storage,
        IUserRepository users,
        ICurrentUserAccessor current,
        IOptions<AppOptions> options)
    {
        _storage = storage;
        _users = users;
        _current = current;
        _options = options.Value;
    }

    /// <summary>
    /// Issues an upload ticket. Frontend then uploads directly to <c>uploadUrl</c>
    /// using <c>uploadMethod</c>. For PUT_BINARY (GCS), publicUrl is known up-front.
    /// For POST_MULTIPART (local), the server returns the URL after the multipart upload.
    /// </summary>
    [HttpPost("upload-ticket")]
    public async Task<IActionResult> CreateTicket([FromBody] UploadTicketRequest req, CancellationToken ct)
    {
        var user = await _users.GetByFirebaseUidAsync(_current.FirebaseUid, ct);
        if (user is null) return Unauthorized(new { error = "User not initialized." });

        try
        {
            var ticket = await _storage.CreateUploadTicketAsync(
                user.Id, req.Kind, req.ContentType, req.SizeBytes, ct);
            return Ok(ticket);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Multipart upload (used by the local provider's ticket flow).</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(60 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 60 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "Missing file." });

        var user = await _users.GetByFirebaseUidAsync(_current.FirebaseUid, ct);
        if (user is null) return Unauthorized(new { error = "User not initialized." });

        try
        {
            await using var stream = file.OpenReadStream();
            var stored = await _storage.SaveAsync(
                user.Id,
                stream,
                file.ContentType ?? "application/octet-stream",
                file.Length,
                file.FileName,
                ct);
            return Ok(new MediaUploadResultDto(stored.Url, stored.ContentType, stored.SizeBytes));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
