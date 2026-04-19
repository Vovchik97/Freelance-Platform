using Microsoft.AspNetCore.Identity;
using Microsoft.Build.Framework;

namespace FreelancePlatform.Models;

public class WorkItem
{
    public int Id { get; set; }
    
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    
    [Required]
    public string Title { get; set; }
    public string? Description { get; set; }

    public WorkItemStatus Status { get; set; } = WorkItemStatus.NotStarted;
    public int OrderIndex { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    public string CreatedById { get; set; }
    public IdentityUser CreatedBy { get; set; }
}