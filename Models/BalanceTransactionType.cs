namespace FreelancePlatform.Models;

/// <summary>
/// Определяет тип операции изменения пользовательского баланса.
/// </summary>
public enum BalanceTransactionType
{
    Deposit = 0,
    Freeze = 1,
    Release = 2,
    Payout = 4,
    Refund = 5,
    Commission = 6,
    Withdraw = 7
}