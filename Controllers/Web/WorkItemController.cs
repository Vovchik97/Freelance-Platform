using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Dto.WorkItems;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

public class WorkItemController : Controller
{
    private readonly WorkItemService _workItemService;
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public WorkItemController(WorkItemService workItemService, AppDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _workItemService = workItemService;
        _context = context;
        _userManager = userManager;
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToProject(int projectId, CreateWorkItemDto dto)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null || project.ClientId != User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            return Forbid();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _workItemService.AddWorkItemAsync(projectId, null, dto, userId);

        TempData["SuccessMessage"] = "Задача добавлена";
        return RedirectToAction("Details", "Project", new { id = projectId });
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToOrder(int orderId, CreateWorkItemDto dto)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null || order.ClientId != User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            return Forbid();
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _workItemService.AddWorkItemAsync(null, orderId, dto, userId);
        
        TempData["SuccessMessage"] = "Задача добавлена";
        return RedirectToAction("Details", "Order", new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int workItemId, WorkItemStatus status, string returnUrl)
    {
        var item = await _context.WorkItems
            .Include(w => w.Project)
            .Include(w => w.Order)
            .FirstOrDefaultAsync(w => w.Id == workItemId);

        if (item == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        bool canUpdate = false;
        if (item.ProjectId.HasValue && item.Project != null)
        {
            canUpdate = item.Project.ClientId == userId || item.Project.SelectedFreelancerId == userId;
        }
        else if (item.OrderId.HasValue && item.Order != null)
        {
            canUpdate = item.Order.ClientId == userId || item.Order.Service?.FreelancerId == userId;
        }

        if (!canUpdate)
        {
            return Forbid();
        }

        await _workItemService.UpdateStatusAsync(workItemId, status);
        
        TempData["SuccessMessage"] = "Статус обновлён";
        return Redirect(returnUrl ?? "/");
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> UpdateOrder([FromBody] UpdateOrderRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (request.ProjectId.HasValue)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.ClientId == userId);

            if (project == null)
            {
                return Forbid();
            }
        }
        else if (request.OrderId.HasValue)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.ClientId == userId);

            if (order == null)
            {
                return Forbid();
            }
        }
        else
        {
            return BadRequest("ProjectId or OrderId должны быть указаны.");  
        }

        for (int i = 0; i < request.WorkItemIds.Count; i++)
        {
            var workItemId = request.WorkItemIds[i];
            var workItem = await _context.WorkItems.FirstOrDefaultAsync(w => w.Id == workItemId);
            if (workItem != null)
            {
                workItem.OrderIndex = i + 1;
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Порядок обновлён" });
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int workItemId, string returnUrl)
    {
        var item = await _context.WorkItems
            .Include(w => w.Project)
            .Include(w => w.Order)
            .FirstOrDefaultAsync(w => w.Id == workItemId);

        if (item == null)
        {
            return NotFound();
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        bool canDelete = false;
        if (item.ProjectId.HasValue && item.Project != null)
        {
            canDelete = item.Project.ClientId == userId;
        }
        else if (item.OrderId.HasValue && item.Order != null)
        {
            canDelete = item.Order.ClientId == userId;
        }

        if (!canDelete)
        {
            return Forbid();
        }

        await _workItemService.DeleteAsync(workItemId);
        
        TempData["SuccessMessage"] = "Задача удалена";
        return Redirect(returnUrl ?? "/");
    }
    
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyTemplateToProject(int projectId, int templateId)
    {
        var project = await _context.Projects.FindAsync(projectId);
        if (project == null || project.ClientId != User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            return Forbid();
        }

        var hasItems = await _context.WorkItems.AnyAsync(w => w.ProjectId == projectId);
        if (hasItems)
        {
            TempData["ErrorMessage"] = "У проекта уже есть задачи";
            return RedirectToAction("Details", "Project", new { id = projectId });
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _workItemService.CreateFromTemplateAsync(templateId, projectId, null, userId);

        project.TaskTemplateId = templateId;
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Шаблон применён";
        return RedirectToAction("Details", "Project", new { id = projectId });
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyTemplateToOrder(int orderId, int templateId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null || order.ClientId != User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            return Forbid();
        }
        
        var hasItems = await _context.WorkItems.AnyAsync(w => w.OrderId == orderId);
        if (hasItems)
        {
            TempData["ErrorMessage"] = "У заказа уже есть задачи";
            return RedirectToAction("Details", "Order", new { id = orderId });
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        await _workItemService.CreateFromTemplateAsync(templateId, null, orderId, userId);
        
        order.TaskTemplateId = templateId;
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Шаблон применён";
        return RedirectToAction("Details", "Order", new { id = orderId });
    }
}