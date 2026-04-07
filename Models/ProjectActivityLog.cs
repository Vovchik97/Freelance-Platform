namespace FreelancePlatform.Models;

public class ProjectActivityLog
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? ActorId { get; set; }
    public string? ActorName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}