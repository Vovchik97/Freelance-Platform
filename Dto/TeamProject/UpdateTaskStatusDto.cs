using FreelancePlatform.Models;

namespace FreelancePlatform.Dto.TeamProject;

public class UpdateTaskStatusDto
{
    public int TaskId { get; set; }
    public ProjectTaskStatus Status { get; set; }
}