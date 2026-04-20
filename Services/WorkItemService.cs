using FreelancePlatform.Context;
using FreelancePlatform.Dto.WorkItems;
using FreelancePlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Services;

public class WorkItemService
{
    private readonly AppDbContext _context;

    public WorkItemService(AppDbContext context)
    {
        _context = context;
    }

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

    public async Task DeleteAsync(int workItemId)
    {
        var item = await _context.WorkItems.FindAsync(workItemId);
        if (item != null)
        {
            _context.WorkItems.Remove(item);
            await _context.SaveChangesAsync();
        }
    }

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