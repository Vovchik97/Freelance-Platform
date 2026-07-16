using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Services;

/// <summary>
/// Предоставляет методы для управления чёрным списком пользователей.
/// </summary>
public class BlacklistService : IBlacklistService
{
    private readonly AppDbContext _context;

    public BlacklistService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Добавляет пользователя в чёрный список.
    /// </summary>
    /// <param name="blockerId">Идентификатор пользователя, выполняющего блокировку.</param>
    /// <param name="blockedId">Идентификатор блокируемого пользователя.</param>
    /// <param name="reason">Причина блокировки.</param>
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

    /// <summary>
    /// Удаляет пользователя из чёрного списка.
    /// </summary>
    /// <param name="blockerId">Идентификатор пользователя, выполняющего разблокировку.</param>
    /// <param name="blockedId">Идентификатор разблокируемого пользователя.</param>
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

    /// <summary>
    /// Проверяет, находится ли пользователь в чёрном списке другого пользователя.
    /// </summary>
    /// <param name="blockerId">Идентификатор владельца чёрного списка.</param>
    /// <param name="blockedId">Идентификатор проверяемого пользователя.</param>
    /// <returns>
    /// <see langword="true"/>, если пользователь находится в чёрном списке; иначе — <see langword="false"/>.
    /// </returns>
    public async Task<bool> IsBlockedAsync(string blockerId, string blockedId)
    {
        return await _context.BlacklistEntries
            .AnyAsync(b => b.BlockerId == blockerId && b.BlockedId == blockedId);
    }

    /// <summary>
    /// Проверяет, заблокирован ли один пользователь другим независимо от направления блокировки.
    /// </summary>
    /// <param name="userId1">Идентификатор первого пользователя.</param>
    /// <param name="userId2">Идентификатор второго пользователя.</param>
    /// <returns>
    /// <see langword="true"/>, если между пользователями существует запись о блокировке; иначе — <see langword="false"/>.
    /// </returns>
    public async Task<bool> IsBlockedEitherWayAsync(string userId1, string userId2)
    {
        return await _context.BlacklistEntries
            .AnyAsync(b =>
                (b.BlockerId == userId1 && b.BlockedId == userId2) ||
                (b.BlockerId == userId2 && b.BlockedId == userId1));
    }

    /// <summary>
    /// Возвращает список пользователей, добавленных в чёрный список.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Коллекция записей чёрного списка.</returns>
    public async Task<List<BlacklistEntry>> GetMyBlacklistAsync(string userId)
    {
        return await _context.BlacklistEntries
            .Where(b => b.BlockerId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }
}