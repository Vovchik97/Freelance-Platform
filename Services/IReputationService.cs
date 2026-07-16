using FreelancePlatform.Models;

namespace FreelancePlatform.Services;

/// <summary>
/// Сервис управления репутацией пользователей.
/// Позволяет добавлять события, получать текущую репутацию и просматривать историю изменений.
/// </summary>
public interface IReputationService
{
    /// <summary>
    /// Добавляет новое событие, влияющее на репутацию пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя, для которого создаётся событие.</param>
    /// <param name="type">Тип события репутации.</param>
    /// <param name="orderId">Идентификатор связанного заказа, если событие относится к заказу.</param>
    /// <param name="projectId">Идентификатор связанного проекта, если событие относится к проекту.</param>
    /// <param name="reason">Дополнительное описание причины изменения репутации.</param>
    Task AddEventAsync(string userId, ReputationEventType type, 
        int? orderId = null, int? projectId = null, string? reason = null);

    /// <summary>
    /// Получает текущую репутацию пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Объект с текущим количеством репутационных баллов пользователя.</returns>
    Task<UserReputation> GetAsync(string userId);

    /// <summary>
    /// Получает историю всех изменений репутации пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Список событий, влияющих на репутацию пользователя.</returns>
    Task<List<ReputationEvent>> GetHistoryAsync(string userId);
}