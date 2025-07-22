using Microsoft.AspNetCore.Identity;

namespace FreelancePlatform.Models;

public class Service
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public required string FreelancerId { get; set; }
    public IdentityUser? Freelancer { get; set; }
    public DateTime CreatedAt { get; set; }
    public ServiceStatus Status { get; set; }
    public string? ClientId { get; set; }
    public IdentityUser? Client { get; set; }
}