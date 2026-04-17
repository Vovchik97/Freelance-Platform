using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FreelancePlatform.Models;

public class Review
{
    public int Id { get; set; }
    
    [Microsoft.Build.Framework.Required]
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    
    [Microsoft.Build.Framework.Required]
    public string UserId { get; set; } = null!;
    public IdentityUser User { get; set; } = null!;
    
    [Range(1, 5)]
    public int Rating { get; set; }
    
    [Range(1, 5)]
    public int QualityRating { get; set; }
    
    [Range(1, 5)]
    public int CommunicationRating { get; set; }
    
    [Range(1, 5)]
    public int DeadlineRating { get; set; } 
    
    [Range(1, 5)]
    public int PriceRating { get; set; }
    
    public string? Comment { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}