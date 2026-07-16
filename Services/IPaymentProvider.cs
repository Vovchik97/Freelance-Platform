namespace FreelancePlatform.Services;

/// <summary>
/// Данные для создания внешней платёжной сессии.
/// Используется для передачи параметров платёжному провайдеру.
/// </summary>
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

/// <summary>
/// Результат создания платёжной сессии у внешнего провайдера.
/// </summary>
public class CreateCheckoutSessionResult
{
    public string SessionId { get; set; } = null!;
    public string SessionUrl { get; set; } = null!;
}

/// <summary>
/// Статус платежа во внешней платёжной системе.
/// </summary>
public enum ExternalPaymentsStatus
{
    Unknown, Pending, Succeeded, Canceled, Failed
}

/// <summary>
/// Интерфейс взаимодействия с внешним платёжным провайдером.
/// Отвечает за создание платежей и получение их текущего состояния.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Создаёт новую платёжную сессию во внешнем сервисе оплаты.
    /// </summary>
    /// <param name="req">Параметры создаваемого платежа: сумма, валюта, ссылки возврата и дополнительные данные.</param>
    /// <returns>Информация о созданной сессии с идентификатором и ссылкой оплаты.</returns>
    Task<CreateCheckoutSessionResult> CreateCheckoutSessionAsync(CreateCheckoutSessionRequest req);
    
    /// <summary>
    /// Получает текущий статус ранее созданной платёжной сессии.
    /// </summary>
    /// <param name="sessionId">Идентификатор платёжной сессии во внешнем провайдере.</param>
    /// <returns>Кортеж с текущим статусом платежа и идентификатором платежной операции, если он был создан провайдером.</returns>
    Task<(ExternalPaymentsStatus status, string? paymentIntentId)> GetSessionStatusAsync(string sessionId);
}