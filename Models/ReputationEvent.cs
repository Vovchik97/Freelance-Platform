namespace FreelancePlatform.Models;

public class ReputationEvent
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;
    
    public ReputationEventType Type { get; set; }
    public int Points { get; set; }
    public string? Reason { get; set; }
    
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}