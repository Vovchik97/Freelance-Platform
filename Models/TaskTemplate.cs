using Microsoft.Build.Framework;

namespace FreelancePlatform.Models;

/// <summary>
/// Представляет шаблон задач для проектов и заказов.
/// </summary>
public class TaskTemplate
{
    public int Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string Description { get; set; }
    public List<TaskTemplateItem> Items { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}