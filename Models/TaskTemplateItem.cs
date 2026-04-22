namespace FreelancePlatform.Models;

public class TaskTemplateItem
{
    public int Id { get; set; }
    public int TaskTemplateId { get; set; }
    public TaskTemplate TaskTemplate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
}