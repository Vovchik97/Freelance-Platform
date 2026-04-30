using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Models;

public class UserMetadata
{
    [Key]
    public string UserId { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
}