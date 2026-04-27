namespace FreelancePlatform.Dto.Profiles;

public class ProjectInfoDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public decimal Budget { get; set; }
    public string? Status { get; set; }
    public int BidsCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}