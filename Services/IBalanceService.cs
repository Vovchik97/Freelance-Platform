using FreelancePlatform.Models;

namespace FreelancePlatform.Services;

/// <summary>
/// Определяет контракт сервиса управления пользовательскими балансами.
/// Поддерживает операции пополнения, заморозки, возврата,
/// выплаты и вывода денежных средств.
/// </summary>
public interface IBalanceService
{
    /// <summary>
    /// Пополняет баланс пользователя и создаёт запись транзакции.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="amount">Сумма пополнения.</param>
    /// <param name="paymentId">Идентификатор платежа.</param>
    Task DepositAsync(string userId, decimal amount, int paymentId);
    
    /// <summary>
    /// Замораживает средства пользователя для оплаты заказа.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="amount">Сумма замораживаемых средств.</param>
    /// <param name="orderId">Идентификатор заказа.</param>
    Task FreezeForOrderAsync(string userId, decimal amount, int orderId);
    
    /// <summary>
    /// Замораживает средства пользователя для оплаты проекта.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="amount">Сумма замораживаемых средств.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    Task FreezeForProjectAsync(string userId, decimal amount, int projectId);
    
    /// <summary>
    /// Возвращает пользователю замороженные средства по заказу.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="amount">Сумма возврата.</param>
    /// <param name="orderId">Идентификатор заказа.</param>
    Task RefundForOrderAsync(string userId, decimal amount, int orderId);
    
    /// <summary>
    /// Возвращает пользователю замороженные средства по проекту.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="amount">Сумма возврата.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    Task RefundForProjectAsync(string userId, decimal amount, int projectId);
    
    /// <summary>
    /// Выполняет возврат средств, ранее внесённых через пополнение баланса.
    /// </summary>
    /// <param name="clientId">Идентификатор пользователя.</param>
    /// <param name="amount">Сумма возврата.</param>
    /// <param name="paymentId">Идентификатор платежа.</param>
    Task RefundDepositAsync(string clientId, decimal amount, int paymentId);
    
    /// <summary>
    /// Переводит средства от клиента исполнителю после завершения заказа
    /// с учётом комиссии платформы.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="freelancerId">Идентификатор исполнителя.</param>
    /// <param name="amount">Сумма выплаты.</param>
    /// <param name="orderId">Идентификатор заказа.</param>
    Task ReleaseForOrderAsync(string clientId, string freelancerId, decimal amount, int orderId);
    
    /// <summary>
    /// Переводит средства от клиента исполнителю после завершения проекта
    /// с учётом комиссии платформы.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="freelancerId">Идентификатор исполнителя.</param>
    /// <param name="amount">Сумма выплаты.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    Task ReleaseForProjectAsync(string clientId, string freelancerId, decimal amount, int projectId);

    /// <summary>
    /// Распределяет оплату между участниками командного проекта
    /// с учётом комиссии платформы.
    /// </summary>
    /// <param name="clientId">Идентификатор клиента.</param>
    /// <param name="payouts">Список выплат участникам проекта.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    Task ReleaseForTeamProjectAsync(string clientId, List<(string UserId, string UserName, decimal Amount)> payouts,
        int projectId);
    
    /// <summary>
    /// Выполняет вывод средств с баланса пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="amount">Сумма вывода.</param>
    /// <param name="paymentId">Идентификатор операции вывода.</param>
    Task WithdrawAsync(string userId, decimal amount, int paymentId);
    
    /// <summary>
    /// Возвращает текущий баланс пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Баланс пользователя.</returns>
    Task<UserBalance> GetAsync(string userId);
}