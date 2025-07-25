﻿using System.ComponentModel.DataAnnotations.Schema;
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
    
    public string? SelectedFreelancerId { get; set; }

    [ForeignKey("SelectedFreelancerId")]
    public IdentityUser? SelectedFreelancer { get; set; }
}