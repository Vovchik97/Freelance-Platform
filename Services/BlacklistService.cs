using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Services;

public class BlacklistService : IBlacklistService
{
    private readonly AppDbContext _context;

    public BlacklistService(AppDbContext context)
    {
        _context = context;
    }

    public async Task BlockAsync(string blockerId, string blockedId, string? reason = null)
    {
        var exists = await _context.BlacklistEntries
            .AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);

        if (exists)
        {
            return;
        }

        _context.BlacklistEntries.Add(new BlacklistEntry
        {
            BlockerId = blockerId,
            BlockedId = blockedId,
            Reason = reason
        });

        await _context.SaveChangesAsync();
    }

    public async Task UnblockAsync(string blockerId, string blockedId)
    {
        var entry = await _context.BlacklistEntries
            .FirstOrDefaultAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);

        if (entry == null)
        {
            return;
        }

        _context.BlacklistEntries.Remove(entry);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsBlockedAsync(string blockerId, string blockedId)
    {
        return await _context.BlacklistEntries
            .AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);
    }

    public async Task<bool> IsBlockedEitherWayAsync(string userId1, string userId2)
    {
        return await _context.BlacklistEntries
            .AnyAsync(b =>
                (b.BlockerId == userId1 && b.BlockedId == userId2) ||
                (b.BlockerId == userId2 && b.BlockedId == userId1));
    }

    public async Task<List<BlacklistEntry>> GetMyBlacklistAsync(string userId)
    {
        return await _context.BlacklistEntries
            .Where(b => b.BlockerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
}