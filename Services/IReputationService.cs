using FreelancePlatform.Models;

namespace FreelancePlatform.Services;

public interface IReputationService
{
    Task AddEventAsync(string userId, ReputationEventType type, 
        int? orderId = null, int? projectId = null, string? reason = null);

    Task<UserReputation> GetAsync(string userId);

    Task<List<ReputationEvent>> GetHistoryAsync(string userId);
}