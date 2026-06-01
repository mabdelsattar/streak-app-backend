using System.ComponentModel.DataAnnotations;

namespace StreakPlatform.Application.DTOs;

public class PurchaseRequest
{
    [Required, StringLength(40)]
    public string PackId { get; set; } = null!;
}
