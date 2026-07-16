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

/// <summary>
/// Контроллер управления заказами пользователей.
/// Позволяет создавать, редактировать, удалять и просматривать заказы,
/// управляет выполнением заказов, связанными задачами,
/// финансовыми операциями и изменением репутации исполнителей.
/// </summary>
public class OrderController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly BalanceService _balanceService;
    private readonly WorkItemService _workItemService;
    private readonly IReputationService _reputationService;
    private readonly IBlacklistService _blacklistService;

    public OrderController(
        AppDbContext context, 
        UserManager<IdentityUser> userManager, 
        IEmailSender emailSender, BalanceService balanceService, 
        WorkItemService workItemService,
        IReputationService reputationService,
        IBlacklistService blacklistService)
    {
        _context = context;
        _userManager = userManager;
        _emailSender = emailSender;
        _balanceService = balanceService;
        _workItemService = workItemService;
        _reputationService = reputationService;
        _blacklistService = blacklistService;
    }

    /// <summary>
    /// Отображает информацию о заказе,
    /// включая исполнителя, категории услуги,
    /// рабочие элементы и прогресс выполнения.
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <returns>Представление страницы заказа или 404, если заказ не найден.</returns>
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
        await LoadTaskTemplatesAsync();

        return View(order);
    }

    /// <summary>
    /// Отображает форму создания заказа по выбранной услуге.
    /// Загружает доступные шаблоны задач и категории услуги.
    /// </summary>
    /// <param name="serviceId">Идентификатор услуги, для которой создаётся заказ.</param>
    /// <returns>Форма создания заказа.</returns>
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Create(int serviceId)
    {
        var dto = new CreateOrderDto
        {
            ServiceId = serviceId
        };
        ViewBag.ServiceId = serviceId;
        await LoadTaskTemplatesAsync();

        var service = await _context.Services
            .Include(s => s.Categories)
            .FirstOrDefaultAsync(s => s.Id == serviceId);

        ViewBag.ServiceCategoryIds = service?.Categories.Select(c => c.Id).ToList() ?? new List<int>();
        return View();
    }

    /// <summary>
    /// Создаёт новый заказ от клиента исполнителю.
    /// Выполняет проверки существующих заказов,
    /// блокировок пользователей, отправляет уведомление
    /// и создаёт задачи из шаблона при необходимости.
    /// </summary>
    /// <param name="dto">Данные создаваемого заказа.</param>
    /// <param name="templateId">Идентификатор шаблона задач. — необязательный параметр.</param>
    /// <returns>Перенаправление на список заказов или возврат формы при ошибке валидации.</returns>
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderDto dto, int? templateId = null)
    {
        if (!ModelState.IsValid)
        {
            await LoadTaskTemplatesAsync();
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

        var isBlocked = await _blacklistService.IsBlockedEitherWayAsync(userId, service.FreelancerId);

        if (isBlocked)
        {
            TempData["ErrorMessage"] = "Вы не можете заказать эту услугу — один из вас заблокировал другого.";
            return RedirectToAction("Details", "Service", new { id = dto.ServiceId });
        }

        var order = new Order
        {
            Comment = dto.Comment,
            DurationInDays = dto.DurationInDays,
            CreatedAt = DateTime.UtcNow,
            ClientId = client.Id,
            ServiceId = dto.ServiceId
        };
        
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        
        var freelancerEmail = service.Freelancer?.Email;

        if (!string.IsNullOrWhiteSpace(freelancerEmail))
        {
            await _emailSender.SendEmailAsync(
                email: freelancerEmail,
                subject: "Новый заказ на услугу",
                htmlMessage: $"Пользователь {client.UserName} сделал заказ на услугу {service.Title}."
            );
        }
        
        if (templateId.HasValue && templateId > 0)
        {
            await _workItemService.CreateFromTemplateAsync(templateId.Value, null, order.Id, client.Id);
        }

        return RedirectToAction(nameof(MyOrders));
    }

    /// <summary>
    /// Отображает форму редактирования заказа клиентом.
    /// </summary>
    /// <param name="id">Идентификатор редактируемого заказа.</param>
    /// <returns>Форма редактирования заказа или 404 если заказ не найден.</returns>
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

    /// <summary>
    /// Изменяет данные существующего заказа.
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <param name="dto">Новые данные заказа.</param>
    /// <returns>Перенаправление после сохранения или отображение формы при ошибке.</returns>
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

    /// <summary>
    /// Удаляет заказ клиента вместе со связанными рабочими элементами.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого заказа.</param>
    /// <returns>Перенаправление на список заказов или 404 если заказ не найден.</returns>
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

    /// <summary>
    /// Отображает список заказов текущего клиента.
    /// </summary>
    /// <returns>Список заказов текущего клиента.</returns>
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
    
    /// <summary>
    /// Завершает заказ,
    /// освобождает средства исполнителю
    /// и начисляет репутацию за выполнение работы.
    /// </summary>
    /// <param name="id">Идентификатор завершаемого заказа.</param>
    /// <returns>Перенаправление на страницу заказа.</returns>
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

        if (order.Service == null)
        {
            return BadRequest();
        }

        var freelancerId = order.Service.FreelancerId;
        await _balanceService.ReleaseForOrderAsync(
            order.ClientId,
            freelancerId,
            order.Service.Price,
            order.Id
        );
        
        order.Status = OrderStatus.Completed;
        await _context.SaveChangesAsync();
        
        await ApplyDeliveryReputationAsync(freelancerId, order.CreatedAt, order.DurationInDays, orderId: order.Id);
        
        TempData["SuccessMessage"] = "Заказ выполнен.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    /// <summary>
    /// Рассчитывает и добавляет событие репутации исполнителю
    /// в зависимости от соблюдения срока выполнения заказа.
    /// </summary>
    /// <param name="freelancerId">Идентификатор исполнителя.</param>
    /// <param name="startedAt">Дата создания заказа.</param>
    /// <param name="durationInDays">Срок выполнения заказа в днях.</param>
    /// <param name="orderId">Идентификатор заказа.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    private async Task ApplyDeliveryReputationAsync(string freelancerId, DateTime startedAt, int durationInDays, int? orderId = null, int? projectId = null)
    {
        var now = DateTime.UtcNow;
        var deadline = startedAt.AddDays(durationInDays);

        ReputationEventType eventType;
        string reason;

        if (durationInDays <= 0)
        {
            // Срок не указан
            eventType = ReputationEventType.OnTimeDelivery;
            reason = "Работа выполнена";
        }
        else if (now < deadline.AddDays(-1))
        {
            // Сдал более чем за день до дедлайна
            eventType = ReputationEventType.EarlyDelivery;
            reason = $"Досрочная сдача (срок {durationInDays} дн., дедлайн {deadline:dd.MM.yyyy})";
        }
        else if (now <= deadline)
        {
            // Сдал в срок
            eventType = ReputationEventType.OnTimeDelivery;
            reason = $"Сдача в срок (срок {durationInDays} дн., дедлайн {deadline:dd.MM.yyyy})";
        }
        else
        {
            // Просрочил
            var daysLate = (int)(now - deadline).TotalDays;
            eventType = ReputationEventType.LateDelivery;
            reason = $"Просрочка на {daysLate} дн. (срок был {durationInDays} дн., дедлайн {deadline:dd.MM.yyyy})";
        }
        
        await _reputationService.AddEventAsync(
            freelancerId,
            eventType,
            orderId,
            projectId,
            reason
        );
    }

    /// <summary>
    /// Загружает шаблоны задач для создания заказа.
    /// </summary>
    private async Task LoadTaskTemplatesAsync()
    {
        ViewBag.TaskTemplates = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .ToListAsync();
    }
}