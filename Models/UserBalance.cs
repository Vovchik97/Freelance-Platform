using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Models;

public class UserBalance
{
    [Key]
    public string UserId { get; set; } = null!;
    public decimal Balance { get; set; }
    public decimal Frozen { get; set; }
}