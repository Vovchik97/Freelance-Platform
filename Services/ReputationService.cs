using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Services;

/// <summary>
/// Сервис управления системой репутации пользователей.
/// Отвечает за начисление и списание репутационных баллов,
/// получение текущего рейтинга и истории изменений.
/// </summary>
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

    /// <summary>
    /// Получает текущую репутацию пользователя.
    /// Если запись отсутствует, создаёт новую с нулевым рейтингом.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Объект текущей репутации пользователя.</returns>
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

    /// <summary>
    /// Добавляет событие репутации и изменяет рейтинг пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя, которому изменяется репутация.</param>
    /// <param name="type">Тип события, определяющий количество начисляемых баллов.</param>
    /// <param name="orderId">Идентификатор связанного заказа, если событие относится к заказу.</param>
    /// <param name="projectId">Идентификатор связанного проекта, если событие относится к проекту.</param>
    /// <param name="reason">Дополнительное описание причины изменения рейтинга.</param>
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

    /// <summary>
    /// Получает историю всех изменений репутации пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <returns>Список событий репутации, отсортированный от новых к старым.</returns>
    public async Task<List<ReputationEvent>> GetHistoryAsync(string userId)
    {
        return await _context.ReputationEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }
}