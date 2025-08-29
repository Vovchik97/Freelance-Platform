namespace FreelancePlatform.Services;

public class CreateCheckoutSessionRequest
{
    public string SuccessUrl { get; set; } = null!;
    public string CancelUrl { get; set; } = null!;
    public string Description { get; set; } = "Оплата заказа";
    public string Currency { get; set; } = "RUB";
    public long AmountMinor { get; set; }
    public string CustomerEmail { get; set; } = null!;
    public string MetadataPaymentId { get; set; } = null!;
}

public class CreateCheckoutSessionResult
{
    public string SessionId { get; set; } = null!;
    public string SessionUrl { get; set; } = null!;
}

public enum ExternalPaymentsStatus
{
    Unknown, Pending, Succeeded, Canceled, Failed
}

public interface IPaymentProvider
{
    Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest req);
    Task<(ExternalPaymentsStatus status, string? paymentIntentId)> GetSessionStatusAsync(string sessionId);
}