﻿using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace FreelancePlatform.Models;

public class Bid
{
    public int Id { get; set; }
    public required int ProjectId { get; set; }
    public Project? Project { get; set; }
    public required string FreelancerId { get; set; }
    public IdentityUser? Freelancer { get; set; }
    public decimal Amount { get; set; }
    public string? Comment { get; set; }
    public int DurationInDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public BidStatus Status { get; set; } = BidStatus.Pending;
}