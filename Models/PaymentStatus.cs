namespace FreelancePlatform.Models;

/// <summary>
/// Определяет текущее состояние платежной операции.
/// </summary>
public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Canceled = 2,
    Failed = 3,
    Refunded = 4
}