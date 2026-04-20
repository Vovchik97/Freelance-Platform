namespace FreelancePlatform.Models;

public class TaskTemplateItem
{
    public int Id { get; set; }
    public int TaskTemplateId { get; set; }
    public TaskTemplate TaskTemplate { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int OrderIndex { get; set; }
}