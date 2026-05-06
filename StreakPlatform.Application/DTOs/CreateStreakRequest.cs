using System.ComponentModel.DataAnnotations;

namespace StreakPlatform.Application.DTOs;

public class CreateStreakRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool RequiresProof { get; set; }
}
