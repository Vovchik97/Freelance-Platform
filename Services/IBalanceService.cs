using FreelancePlatform.Models;

namespace FreelancePlatform.Services;

public interface IBalanceService
{
    Task DepositAsync(string userId, decimal amount, int paymentId);
    
    Task FreezeForOrderAsync(string userId, decimal amount, int orderId);
    Task FreezeForProjectAsync(string userId, decimal amount, int projectId);
    
    Task RefundForOrderAsync(string userId, decimal amount, int orderId);
    Task RefundForProjectAsync(string userId, decimal amount, int projectId);
    Task RefundDepositAsync(string clientId, decimal amount, int paymentId);
    
    Task ReleaseForOrderAsync(string clientId, string freelancerId, decimal amount, int orderId);
    Task ReleaseForProjectAsync(string clientId, string freelancerId, decimal amount, int projectId);

    Task ReleaseForTeamProjectAsync(string clientId, List<(string UserId, string UserName, decimal Amount)> payouts,
        int projectId);
    
    Task WithdrawAsync(string userId, decimal amount, int paymentId);
    
    Task<UserBalance> GetAsync(string userId);
}