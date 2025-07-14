using Microsoft.AspNetCore.Identity;

namespace FreelancePlatform.Models;

public class Project
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public decimal Budget { get; set; }
    public required string ClientId { get; set; }
    public IdentityUser? Client { get; set; }
    public DateTime CreatedAt { get; set; }
    public ProjectStatus Status { get; set; }
}