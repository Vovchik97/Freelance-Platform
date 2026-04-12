using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

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
    public List<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    
    public string? SelectedFreelancerId { get; set; }

    [ForeignKey("SelectedFreelancerId")]
    public IdentityUser? SelectedFreelancer { get; set; }

    public bool IsTeamProject { get; set; } = false;
    public List<ProjectMember> Members { get; set; } = new();
    public List<ProjectTask> Tasks { get; set; } = new();
    public List<ProjectActivityLog> ActivityLogs { get; set; } = new();
    public List<GroupChatMessage> GroupChatMessages { get; set; } = new();
    public ICollection<PaymentShare> PaymentShares { get; set; } = new List<PaymentShare>();
}