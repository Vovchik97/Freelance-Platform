namespace FreelancePlatform.Models;

/// <summary>
/// Определяет тип события, влияющего на репутацию пользователя.
/// </summary>
public enum ReputationEventType
{
    EarlyDelivery = 0,
    OnTimeDelivery = 1,
    LateDelivery = 2,
    PositiveReview = 3,
    NegativeReview = 4,
    OrderCancelled = 5,
    ReportResolved = 6
}