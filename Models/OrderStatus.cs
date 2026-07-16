namespace FreelancePlatform.Models;

/// <summary>
/// Определяет текущее состояние заказа.
/// </summary>
public enum OrderStatus
{
    Pending,
    Accepted,
    Paid,
    Rejected,
    Completed
}