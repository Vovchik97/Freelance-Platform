using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace FreelancePlatform.Models;

public class UserProfile
{
    [Key]
    [ForeignKey("User")]
    public string UserId { get; set; } = null!;
    public IdentityUser User { get; set; } = null!;
    public string AboutMe { get; set; } = string.Empty;
}