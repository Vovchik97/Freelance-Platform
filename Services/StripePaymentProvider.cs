using Stripe;
using Stripe.Checkout;

namespace FreelancePlatform.Services;

public class StripePaymentProvider : IPaymentProvider
{
    public StripePaymentProvider(string apiKey)
    {
        StripeConfiguration.ApiKey = apiKey;
    }

    public async Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest req)
    {
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = req.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = req.CancelUrl,
            CustomerEmail = req.CustomerEmail,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = req.AmountMinor,
                        Currency = req.Currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = req.Description
                        }
                    },
                    Quantity = 1
                }
            },
            Metadata = new Dictionary<string, string>
            {
                { "payment_id", req.MetadataPaymentId }
            }
        };
        
        var service = new SessionService();
        var session = await service.CreateAsync(options);

        return new CreateCheckoutSessionResult
        {
            SessionId = session.Id,
            SessionUrl = session.Url
        };
    }

    public async Task<(ExternalPaymentsStatus status, string? paymentIntentId)> GetSessionStatusAsync(string sessionId)
    {
        var service = new SessionService();
        var session = await service.GetAsync(sessionId);
        var pi = session.PaymentIntentId;

        return session.PaymentStatus switch
        {
            "paid" => (ExternalPaymentsStatus.Succeeded, pi),
            _ when session.Status == "expired" => (ExternalPaymentsStatus.Canceled, pi),
            _ => (ExternalPaymentsStatus.Pending, pi)
        };
    }
}