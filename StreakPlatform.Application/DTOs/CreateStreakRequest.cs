using System.ComponentModel.DataAnnotations;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.DTOs;

public class CreateStreakRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public CheckInType CheckInType { get; set; } = CheckInType.Action;

    [StringLength(40)]
    public string? CheckInButtonLabel { get; set; }

    /// <summary>
    /// If true, the streak appears in the public Discover list. Anyone can browse and join it.
    /// If false (default), the streak is only joinable via invite code/URL.
    /// </summary>
    public bool IsPublic { get; set; }
}
