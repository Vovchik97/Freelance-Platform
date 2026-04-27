using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Models;

public class UserReputation
{
    [Key]
    public string UserId { get; set; } = null!;
    public int Score { get; set; } = 0;
}