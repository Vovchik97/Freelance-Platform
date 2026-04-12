namespace FreelancePlatform.Models;

public class PaymentShare
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsLead { get; set; }
    
    public int TasksDone { get; set; }
    public decimal AutoShare { get; set; }
    public decimal FinalShare { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}