using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Dto.TeamProject;

public class CreateTaskDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required(ErrorMessage = "Название задачи обязательно")]
    public string Title { get; set; } = null!;
    
    public string Description { get; set; }
    
    public string? AssignedToUserId { get; set; }
}