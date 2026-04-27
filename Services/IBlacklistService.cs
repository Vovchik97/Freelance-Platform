using FreelancePlatform.Models;

namespace FreelancePlatform.Services;

public interface IBlacklistService
{
    Task BlockAsync(string blockerId, string blockedId, string? reason = null);
    Task UnblockAsync(string blockerId, string blockedId);
    Task<bool> IsBlockedAsync(string blockerId, string blockedId);

    Task<bool> IsBlockedEitherWayAsync(string userId1, string userId2);

    Task<List<BlacklistEntry>> GetMyBlacklistAsync(string userId);
}