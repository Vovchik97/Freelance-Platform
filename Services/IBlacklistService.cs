using FreelancePlatform.Models;

namespace FreelancePlatform.Services;

/// <summary>
/// Определяет контракт сервиса управления чёрным списком пользователей.
/// Позволяет добавлять пользователей в чёрный список,
/// удалять блокировки и проверять наличие ограничений между пользователями.
/// </summary>
public interface IBlacklistService
{
    /// <summary>
    /// Добавляет пользователя в чёрный список другого пользователя.
    /// </summary>
    /// <param name="blockerId">Идентификатор пользователя, устанавливающего блокировку.</param>
    /// <param name="blockedId">Идентификатор пользователя, которого необходимо заблокировать.</param>
    /// <param name="reason">Причина добавления в чёрный список.</param>
    Task BlockAsync(string blockerId, string blockedId, string? reason = null);
    
    /// <summary>
    /// Удаляет пользователя из чёрного списка.
    /// </summary>
    /// <param name="blockerId">Идентификатор пользователя, установившего блокировку.</param>
    /// <param name="blockedId">Идентификатор пользователя, которого необходимо разблокировать.</param>
    Task UnblockAsync(string blockerId, string blockedId);
    
    /// <summary>
    /// Проверяет, заблокирован ли один пользователь другим.
    /// </summary>
    /// <param name="blockerId">Идентификатор пользователя, владеющего чёрным списком.</param>
    /// <param name="blockedId">Идентификатор проверяемого пользователя.</param>
    /// <returns>
    /// <see langword="true"/>, если пользователь находится в чёрном списке;
    /// иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> IsBlockedAsync(string blockerId, string blockedId);

    /// <summary>
    /// Проверяет наличие блокировки между двумя пользователями независимо от направления.
    /// </summary>
    /// <param name="userId1">Идентификатор первого пользователя.</param>
    /// <param name="userId2">Идентификатор второго пользователя.</param>
    /// <returns>
    /// <see langword="true"/>, если один из пользователей заблокировал другого;
    /// иначе — <see langword="false"/>.
    /// </returns>
    Task<bool> IsBlockedEitherWayAsync(string userId1, string userId2);

    /// <summary>
    /// Возвращает список пользователей, добавленных текущим пользователем в чёрный список.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Список записей чёрного списка пользователя.</returns>
    Task<List<BlacklistEntry>> GetMyBlacklistAsync(string userId);
}