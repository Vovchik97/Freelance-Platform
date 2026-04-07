namespace FreelancePlatform.Models;

public class ProjectMember
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Member;
    public ProjectMemberStatus Status { get; set; } = ProjectMemberStatus.Pending;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}