using FreelancePlatform.Context;
using FreelancePlatform.Models;

namespace FreelancePlatform.Services;

/// <summary>
/// Сервис для создания записей истории действий внутри проектов.
/// Используется для фиксации изменений состояния проекта,
/// действий участников и других значимых событий.
/// </summary>
public class ProjectActivityLogService
{
    private readonly AppDbContext _context;

    public ProjectActivityLogService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Добавляет новую запись в журнал активности проекта.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта, для которого создаётся запись.</param>
    /// <param name="action">Описание выполненного действия.</param>
    /// <param name="actorId">Идентификатор пользователя, выполнившего действие.</param>
    /// <param name="actorName">Имя пользователя, выполнившего действие.</param>
    public async Task LogAsync(int projectId, string action, string? actorId = null, string? actorName = null)
    {
        var log = new ProjectActivityLog
        {
            ProjectId = projectId,
            Action = action,
            ActorId = actorId,
            ActorName = actorName,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}