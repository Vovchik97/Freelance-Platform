using Stripe;
using Stripe.Checkout;

namespace FreelancePlatform.Services;

/// <summary>
/// Реализация платежного провайдера через Stripe.
/// Создаёт платёжные сессии и получает их текущее состояние.
/// </summary>
public class StripePaymentProvider : IPaymentProvider
{
    public StripePaymentProvider(string apiKey)
    {
        StripeConfiguration.ApiKey = apiKey;
    }

    /// <summary>
    /// Создаёт новую Checkout-сессию Stripe для оплаты.
    /// </summary>
    /// <param name="req">Параметры создаваемой платежной сессии.</param>
    /// <returns>Данные созданной платежной сессии.</returns>
    /// <exception cref="ArgumentNullException">Возникает, если запрос не был передан.</exception>
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

    /// <summary>
    /// Получает текущее состояние Checkout-сессии Stripe.
    /// </summary>
    /// <param name="sessionId">Идентификатор платежной сессии.</param>
    /// <returns>Статус платежа и идентификатор PaymentIntent.</returns>
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