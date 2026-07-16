namespace FreelancePlatform.Models;

/// <summary>
/// Определяет тип платежной операции.
/// </summary>
public enum PaymentType
{
    Order = 0,
    Project = 1,
    Deposit = 2,
    Withdrawal = 3
}