using FreelancePlatform.Models;

namespace FreelancePlatform.Dto.WorkItems;

public class WorkItemDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public WorkItemStatus Status { get; set; }
    public int OrderIndex { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CreatedByName { get; set; }
}