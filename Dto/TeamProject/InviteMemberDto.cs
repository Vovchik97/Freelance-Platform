using System.ComponentModel.DataAnnotations;
using FreelancePlatform.Models;

namespace FreelancePlatform.Dto.TeamProject;

public class InviteMemberDto
{
    [Required]
    public int ProjectId { get; set; }

    [Required] public string UserName { get; set; } = null!;

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Member;
}