namespace FreelancePlatform.Models;

public class Payment
{
    public int Id { get; set; }
    
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public string PayerId { get; set; } = null!;
    public long AmountMinor { get; set; }
    public string Currency { get; set; } = "RUB";

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    public string? Provider { get; set; }
    public string? ProviderSessionId { get; set; }
    public string? ProviderPaymentIntentId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}