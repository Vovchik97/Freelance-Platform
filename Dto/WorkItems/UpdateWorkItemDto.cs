using FreelancePlatform.Models;

namespace FreelancePlatform.Dto.WorkItems;

public class UpdateWorkItemDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public WorkItemStatus Status { get; set; }
}