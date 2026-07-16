using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Models;

/// <summary>
/// Хранит текущий рейтинг репутации пользователя.
/// </summary>
public class UserReputation
{
    [Key]
    public string UserId { get; set; } = null!;
    public int Score { get; set; } = 0;
}