using System.ComponentModel.DataAnnotations;

namespace StreakPlatform.Application.DTOs;

// Future-ready: custom streaks endpoint will consume this.
public class CreateStreakDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = null!;
}
