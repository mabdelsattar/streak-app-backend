using System.ComponentModel.DataAnnotations;

namespace StreakPlatform.Application.DTOs;

public class JoinStreakRequest
{
    [Required, StringLength(16, MinimumLength = 4)]
    public string InviteCode { get; set; } = null!;
}
