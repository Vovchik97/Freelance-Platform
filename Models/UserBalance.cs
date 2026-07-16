using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Models;

/// <summary>
/// Представляет информацию о текущем и замороженном балансе пользователя.
/// </summary>
public class UserBalance
{
    [Key]
    public string UserId { get; set; } = null!;
    public decimal Balance { get; set; }
    public decimal Frozen { get; set; }
}