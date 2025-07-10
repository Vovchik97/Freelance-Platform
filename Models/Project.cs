namespace FreelancePlatform.Models;

public class Project
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Budget { get; set; }
    public int ClientId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; }
}