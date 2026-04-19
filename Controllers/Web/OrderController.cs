using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Dto;
using FreelancePlatform.Dto.Bids;
using FreelancePlatform.Dto.Orders;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

public class OrderController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly BalanceService _balanceService;
    private readonly WorkItemService _workItemService;

    public OrderController(AppDbContext context, UserManager<IdentityUser> userManager, IEmailSender emailSender, BalanceService balanceService, WorkItemService workItemService)
    {
        _context = context;
        _userManager = userManager;
        _emailSender = emailSender;
        _balanceService = balanceService;
        _workItemService = workItemService;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Service)
                .ThenInclude(s => s!.Freelancer)
            .Include(o => o.Service)
                .ThenInclude(s => s!.Categories)
            .Include(o => o.WorkItems)
                .ThenInclude(w => w.CreatedBy)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }
        
        var progress = await _workItemService.GetProgressAsync(null, id);

        ViewBag.Progress = progress;
        ViewBag.WorkItems = order.WorkItems.OrderBy(w => w.OrderIndex).ToList();
        ViewBag.TaskTemplates = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .ToListAsync();

        return View(order);
    }

    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Create(int serviceId)
    {
        var dto = new CreateOrderDto
        {
            ServiceId = serviceId
        };
        ViewBag.ServiceId = serviceId;
        ViewBag.TaskTemplates = _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .ToList();

        var service = await _context.Services
            .Include(s => s.Categories)
            .FirstOrDefaultAsync(s => s.Id == serviceId);

        ViewBag.ServiceCategoryIds = service?.Categories.Select(c => c.Id).ToList() ?? new List<int>();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderDto dto, int? templateId = null)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.TaskTemplates = await _context.TaskTemplates
                .Include(t => t.Items)
                .Include(t => t.Categories)
                .ToListAsync();
            ViewBag.ServiceId = dto.ServiceId;
            return View(dto);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var service = await _context.Services
            .Include(s => s.Freelancer)
            .Include(s => s.Orders)
                .ThenInclude(o => o.Client)
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId);
        var client = await _userManager.GetUserAsync(User);

        var hasActiveOrder = service?.Orders?.Any(o => o.Client!.Id == userId && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Accepted)) ?? false;

        if (hasActiveOrder)
        {
            ModelState.AddModelError(string.Empty, "Вы уже отправили заказ на эту услугу.");
            ViewBag.TaskTemplates = await _context.TaskTemplates
                .Include(t => t.Items)
                .Include(t => t.Categories)
                .ToListAsync();
            ViewBag.ServiceId = dto.ServiceId;
            return View(dto);
        }

        if (service == null)
        {
            ModelState.AddModelError(string.Empty, "Услуга не найдена.");
            ViewBag.TaskTemplates = await _context.TaskTemplates
                .Include(t => t.Items)
                .Include(t => t.Categories)
                .ToListAsync();
            ViewBag.ServiceId = dto.ServiceId;
            return View(dto);
        }
        
        if (service.Freelancer == null)
        {
            ModelState.AddModelError(string.Empty, "Фрилансер не найден.");
            ViewBag.TaskTemplates = await _context.TaskTemplates
                .Include(t => t.Items)
                .Include(t => t.Categories)
                .ToListAsync();
            ViewBag.ServiceId = dto.ServiceId;
            return View(dto);
        }
        
        if (client == null)
        {
            return Unauthorized("Пользователь не найден.");
        }

        var order = new Order
        {
            Comment = dto.Comment,
            DurationInDays = dto.DurationInDays,
            CreatedAt = DateTime.UtcNow,
            ClientId = client.Id,
            ServiceId = dto.ServiceId
        };
        
        var freelancerEmail = service.Freelancer?.Email;

        if (!string.IsNullOrWhiteSpace(freelancerEmail))
        {
            await _emailSender.SendEmailAsync(
                email: freelancerEmail,
                subject: "Новый заказ на услугу",
                htmlMessage: $"Пользователь {client.UserName} сделал заказ на услугу {service.Title}."
            );
        }

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        
        if (templateId.HasValue && templateId > 0)
        {
            await _workItemService.CreateFromTemplateAsync(templateId.Value, null, order.Id, client.Id);
        }

        return RedirectToAction(nameof(MyOrders));
    }

    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ClientId == userId);

        if (order == null)
        {
            return NotFound();
        }

        var dto = new UpdateOrderDto
        {
            Comment = order.Comment,
            DurationInDays = order.DurationInDays
        };

        return View(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ClientId == userId);

        if (order == null)
        {
            return NotFound();
        }

        order.Comment = dto.Comment;
        order.DurationInDays = dto.DurationInDays;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(MyOrders));
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _context.Orders
            .Include(o => o.WorkItems)
            .FirstOrDefaultAsync(o => o.Id == id && o.ClientId == userId);

        if (order == null)
        {
            return NotFound();
        }
        
        if (order.WorkItems != null && order.WorkItems.Any())
        {
            _context.WorkItems.RemoveRange(order.WorkItems);
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(MyOrders));
    }

    [Authorize(Roles = "Client")]
    public async Task<IActionResult> MyOrders()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var myOrders = await _context.Orders
            .Include(o => o.Service)
            .Where(o => o.ClientId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return View(myOrders);
    }
    
    [HttpPost]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> CompleteOrder(int id)
    {
        var userId = _userManager.GetUserId(User);
        var order = await _context.Orders
            .Include(o => o.Service)
            .FirstOrDefaultAsync(o => o.Id == id);
        
        if (order == null)
        {
            return NotFound();
        }

        if (order.ClientId != userId)
        {
            return Forbid();
        }

        var freelancerId = order.Service!.FreelancerId!;
        await _balanceService.ReleaseForOrderAsync(
            order.ClientId,
            freelancerId,
            order.Service.Price,
            order.Id
        );
        
        order.Status = OrderStatus.Completed;
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Заказ выполнен.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }
}