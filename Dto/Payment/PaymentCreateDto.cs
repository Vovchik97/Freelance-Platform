namespace FreelancePlatform.Dto.Payment;

public class PaymentCreateDto
{
    public int OrderId { get; set; }
    public string ServiceTitle { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "RUB";
}