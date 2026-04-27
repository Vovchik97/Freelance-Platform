using FreelancePlatform.Models;

namespace FreelancePlatform.Dto.Profiles;

public class PublicProfileDto
{
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string AboutMe { get; set; } = string.Empty;
    
    public double? AverageRating { get; set; }
    public int ReviewsCount { get; set; }
    
    public double? AvgQuality { get; set; }
    public double? AvgCommunication { get; set; }
    public double? AvgDeadline { get; set; }
    public double? AvgPrice { get; set; }
    
    public int ReputationScore { get; set; }
    public List<ReputationEvent> ReputationHistory { get; set; } = new();
    public bool IsBlockedByMe { get; set; }
    public bool IsFreelancer { get; set; }
    
    public List<ServiceInfoDto> Services { get; set; } = new();
    public List<ProjectInfoDto> Projects { get; set; } = new();
}