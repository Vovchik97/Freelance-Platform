namespace FreelancePlatform.Dto.Profiles;

public class PublicProfileDto
{
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string AboutMe { get; set; } = string.Empty;
    
    public double? AverageRating { get; set; }
    public int ReviewsCount { get; set; }
    
    public List<ServiceInfoDto> Services { get; set; } = new();
}