using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace FreelancePlatform.Models;

public class Order
{
    public int Id { get; set; }
    public required int ServiceId { get; set; }
    public Service? Service { get; set; }
    public required string ClientId { get; set; }
    public IdentityUser? Client { get; set; }
    public string? Comment { get; set; }
    public int DurationInDays { get; set; }
    public DateTime CreatedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
}