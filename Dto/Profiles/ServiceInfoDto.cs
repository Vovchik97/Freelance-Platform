namespace FreelancePlatform.Dto.Profiles;

public class ServiceInfoDto
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string FreelancerId { get; set; } = null!;
    public decimal Price { get; set; }
    public string? Status { get; set; }
    public double? Rating { get; set; } = null;
    public int OrdersCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}