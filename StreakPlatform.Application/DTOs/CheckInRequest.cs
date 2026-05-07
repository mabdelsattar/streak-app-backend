using System.ComponentModel.DataAnnotations;

namespace StreakPlatform.Application.DTOs;

public class CheckInRequest
{
    [StringLength(500)]
    public string? Note { get; set; }

    [StringLength(500)]
    public string? MediaUrl { get; set; }

    [StringLength(100)]
    public string? MediaContentType { get; set; }

    [Range(0, 3600)]
    public int? MediaDurationSeconds { get; set; }
}
