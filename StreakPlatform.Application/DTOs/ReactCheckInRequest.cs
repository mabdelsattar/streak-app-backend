using System.ComponentModel.DataAnnotations;
using StreakPlatform.Domain.Entities;

namespace StreakPlatform.Application.DTOs;

public class ReactCheckInRequest
{
    [Required]
    public ReactionType Type { get; set; }
}
