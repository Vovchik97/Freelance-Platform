namespace FreelancePlatform.Dto.Recommendations;

public class RecommendationDto
{
    public int Id { get; set; }
    public string Title { get; set; }
    public decimal Budget { get; set; }
    public double Score { get; set; }
    public List<string> Categories { get; set; }
    public List<string> Reasons { get; set; }
    public string Type { get; set; }
}