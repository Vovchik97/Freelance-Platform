using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Services;

public class ReputationService : IReputationService
{
    private readonly AppDbContext _context;

    private static readonly Dictionary<ReputationEventType, int> Points = new()
    {
        { ReputationEventType.EarlyDelivery, +10 },
        { ReputationEventType.OnTimeDelivery, +5 },
        { ReputationEventType.LateDelivery, -10 },
        { ReputationEventType.PositiveReview, +5 },
        { ReputationEventType.NegativeReview, -5 },
        { ReputationEventType.OrderCancelled, -3 },
        { ReputationEventType.ReportResolved, -20 },
    };

    public ReputationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserReputation> GetAsync(string userId)
    {
        var rep = await _context.UserReputations.FindAsync(userId);
        if (rep == null)
        {
            rep = new UserReputation { UserId = userId, Score = 0 };
            _context.UserReputations.Add(rep);
            await _context.SaveChangesAsync();
        }

        return rep;
    }

    public async Task AddEventAsync(string userId, ReputationEventType type, 
        int? orderId = null, int? projectId = null, string? reason = null)
    {
        var rep = await GetAsync(userId);
        var points = Points[type];

        rep.Score += points;

        _context.ReputationEvents.Add(new ReputationEvent
        {
            UserId = userId,
            Type = type,
            Points = points,
            Reason = reason,
            OrderId = orderId,
            ProjectId = projectId
        });

        await _context.SaveChangesAsync();
    }

    public async Task<List<ReputationEvent>> GetHistoryAsync(string userId)
    {
        return await _context.ReputationEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
}