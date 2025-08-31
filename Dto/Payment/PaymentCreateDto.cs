namespace FreelancePlatform.Dto.Payment;

public class PaymentCreateDto
{
    public int? OrderId { get; set; }
    public int? ProjectId { get; set; }
    public string Title { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
}