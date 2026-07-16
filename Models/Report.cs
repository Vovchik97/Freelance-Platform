namespace FreelancePlatform.Models;

/// <summary>
/// Представляет жалобу пользователя на другого пользователя,
/// связанную с заказом или проектом.
/// </summary>
public class Report
{
    public int Id { get; set; }
    
    public string ReporterId { get; set; } = null!;
    public string ReportedId { get; set; } = null!;
    
    public ReportReason Reason { get; set; }
    public string? Description { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public string? AdminComment { get; set; }
    
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}