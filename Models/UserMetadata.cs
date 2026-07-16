using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Models;

/// <summary>
/// Содержит служебную информацию о пользователе.
/// </summary>
public class UserMetadata
{
    [Key]
    public string UserId { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
}