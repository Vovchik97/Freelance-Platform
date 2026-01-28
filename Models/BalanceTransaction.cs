namespace FreelancePlatform.Models;

public class BalanceTransaction
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = null!;
    public decimal Amount { get; set; }
    
    public BalanceTransactionType Type { get; set; }
    
    public int? PaymentId { get; set; }
    public int? ProjectId { get; set; }
    public int? OrderId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}