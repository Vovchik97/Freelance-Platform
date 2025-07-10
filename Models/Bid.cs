namespace FreelancePlatform.Models;

public class Bid
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int FreelancerId { get; set; }
    public decimal Amount { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}