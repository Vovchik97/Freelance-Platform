namespace FreelancePlatform.Models;

/// <summary>
/// Представляет запись о пользователе, добавленном в черный список.
/// </summary>
public class BlacklistEntry
{
    public int Id { get; set; }

    public string BlockerId { get; set; } = null!;
    public string BlockedId { get; set; } = null!;
    
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}