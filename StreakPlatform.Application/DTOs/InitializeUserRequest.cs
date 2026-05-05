using System.ComponentModel.DataAnnotations;

namespace StreakPlatform.Application.DTOs;

public class InitializeUserRequest
{
    [StringLength(60)]
    public string? DisplayName { get; set; }
}
