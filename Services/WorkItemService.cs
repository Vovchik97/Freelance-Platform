using FreelancePlatform.Context;
using FreelancePlatform.Dto.WorkItems;
using FreelancePlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Services;

/// <summary>
/// Сервис управления рабочими этапами проектов и заказов.
/// Отвечает за создание, изменение, удаление и получение WorkItem.
/// </summary>
public class WorkItemService
{
    private readonly AppDbContext _context;

    public WorkItemService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Создаёт рабочие этапы на основе шаблона задачи.
    /// </summary>
    /// <param name="templateId">Идентификатор шаблона.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="orderId">Идентификатор заказа.</param>
    /// <param name="createdById">Пользователь, создавший этапы.</param>
    public async Task CreateFromTemplateAsync(int templateId, int? projectId, int? orderId, string createdById)
    {
        var template = await _context.TaskTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == templateId);

        if (template == null)
        {
            return;
        }

        var workItems = template.Items
            .OrderBy(i => i.OrderIndex)
            .Select(i => new WorkItem
            {
                ProjectId = projectId,
                OrderId = orderId,
                Title = i.Title,
                Description = i.Description,
                OrderIndex = i.OrderIndex,
                Status = WorkItemStatus.NotStarted,
                CreatedById = createdById,
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        await _context.WorkItems.AddRangeAsync(workItems);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Добавляет новый рабочий этап в проект или заказ.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="orderId">Идентификатор заказа.</param>
    /// <param name="dto">Данные нового этапа.</param>
    /// <param name="createdById">Пользователь, создавший этап.</param>
    /// <returns>Созданный рабочий этап.</returns>
    public async Task<WorkItem> AddWorkItemAsync(int? projectId, int? orderId, CreateWorkItemDto dto,
        string createdById)
    {
        var maxOrder = await _context.WorkItems
            .Where(w => (projectId.HasValue && w.ProjectId == projectId) ||
                        (orderId.HasValue && w.OrderId == orderId))
            .MaxAsync(w => (int?)w.OrderIndex) ?? 0;

        var workItem = new WorkItem
        {
            ProjectId = projectId,
            OrderId = orderId,
            Title = dto.Title,
            Description = dto.Description,
            OrderIndex = maxOrder + 1,
            Status = WorkItemStatus.NotStarted,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.WorkItems.Add(workItem);
        await _context.SaveChangesAsync();
        return workItem;
    }

    /// <summary>
    /// Обновляет статус рабочего этапа.
    /// </summary>
    /// <param name="workItemId">Идентификатор этапа.</param>
    /// <param name="status">Новый статус.</param>
    public async Task UpdateStatusAsync(int workItemId, WorkItemStatus status)
    {
        var item = await _context.WorkItems.FindAsync(workItemId);
        if (item == null)
        {
            return;
        }

        item.Status = status;
        if (status == WorkItemStatus.Completed)
        {
            item.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Возвращает процент выполнения проекта или заказа.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="orderId">Идентификатор заказа.</param>
    /// <returns>Процент выполнения от 0 до 100.</returns>
    public async Task<double> GetProgressAsync(int? projectId, int? orderId)
    {
        var items = await _context.WorkItems
            .Where(w => (projectId.HasValue && w.ProjectId == projectId) ||
                        (orderId.HasValue && w.OrderId == orderId))
            .ToListAsync();

        if (!items.Any())
        {
            return 0;
        }

        var completed = items.Count(w => w.Status == WorkItemStatus.Completed);
        return (completed * 100.0) / items.Count;
    }

    /// <summary>
    /// Удаляет рабочий этап.
    /// </summary>
    /// <param name="workItemId">Идентификатор этапа.</param>
    public async Task DeleteAsync(int workItemId)
    {
        var item = await _context.WorkItems.FindAsync(workItemId);
        if (item != null)
        {
            _context.WorkItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Возвращает список рабочих этапов проекта или заказа.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="orderId">Идентификатор заказа.</param>
    /// <returns>Список этапов.</returns>
    public async Task<List<WorkItem>> GetWorkItemsAsync(int? projectId, int? orderId)
    {
        return await _context.WorkItems
            .Include(w => w.CreatedBy)
            .Where(w => (projectId.HasValue && w.ProjectId == projectId) ||
                        (orderId.HasValue && w.OrderId == orderId))
            .OrderBy(w => w.OrderIndex)
            .ToListAsync();
    }
}