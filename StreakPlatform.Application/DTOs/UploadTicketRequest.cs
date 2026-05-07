using System.ComponentModel.DataAnnotations;

namespace StreakPlatform.Application.DTOs;

public class UploadTicketRequest
{
    [Required, RegularExpression("^(image|audio|video)$", ErrorMessage = "kind must be image, audio, or video")]
    public string Kind { get; set; } = null!;

    [Required, StringLength(100)]
    public string ContentType { get; set; } = null!;

    [Range(1, long.MaxValue)]
    public long SizeBytes { get; set; }
}
