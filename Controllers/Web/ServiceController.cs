using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Api;
using FreelancePlatform.Dto.Projects;
using FreelancePlatform.Dto.Services;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

public class ServiceController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ServiceController(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, string? status, decimal? minBudget, decimal? maxBudget, string sort)
    {
        var query = _context.Services.Include(s => s.Freelancer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Title.Contains(search) || s.Description.Contains(search));
        }
        
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ServiceStatus>(status, out var parsedStatus))
        {
            query = query.Where(s => s.Status == parsedStatus);
        }
        
        if (minBudget.HasValue)
        {
            query = query.Where(s => s.Price >= minBudget);    
        }

        if (maxBudget.HasValue)
        {
            query = query.Where(s => s.Price <= maxBudget);
        }
        
        if (sort == "budget_desc")
            query = query.OrderByDescending(s => s.Price);
        else if (sort == "budget_asc")
            query = query.OrderBy(s => s.Price);
        else
            query = query.OrderByDescending(s => s.CreatedAt);
        
        var services = await query.ToListAsync();

        ViewBag.Searcg = search;
        ViewBag.Status = status;
        ViewBag.MinBudget = minBudget;
        ViewBag.MaxBudget = maxBudget;
        ViewBag.Sort = sort;
        
        return View(services);
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var service = await _context.Services
            .Include(s => s.Freelancer)
            .Include(s => s.Orders)
                .ThenInclude(o => o.Client)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
        {
            return NotFound();
        }
        
        return View(service);
    }

    [Authorize(Roles = "Freelancer")]
    public IActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateServiceDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var service = new Service
        {
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            Status = dto.Status,
            FreelancerId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            CreatedAt = DateTime.UtcNow
        };
        
        await _context.Services.AddAsync(service);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(MyServices));
    }
    
    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id && s.FreelancerId == userId);
        
        if (service == null)
        {
            return NotFound();
        }

        var dto = new UpdateServiceDto
        {
            Title = service.Title,
            Description = service.Description,
            Price = service.Price,
            Status = service.Status
        };
        
        return View(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateServiceDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id && s.FreelancerId == userId);

        if (service == null)
        {
            return NotFound();
        }
        
        service.Title = dto.Title;
        service.Description = dto.Description;
        service.Price = dto.Price;
        service.Status = dto.Status;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(MyServices));
    }

    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == id && s.FreelancerId == userId);

        if (service == null)
        {
            return NotFound();
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(MyServices));
    }

    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> MyServices()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var myServices = await _context.Services
            .Where(s => s.FreelancerId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return View(myServices);
    }

    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptOrder(int serviceId, int orderId)
    {
        var service = await _context.Services
            .Include(s => s.Orders)
            .FirstOrDefaultAsync(s => s.Id == serviceId);
        
        if (service == null)
        {
            return NotFound();
        }

        if (service.FreelancerId != _userManager.GetUserId(User))
        {
            return Forbid();
        }

        var order = service.Orders.FirstOrDefault(o => o.Id == orderId);
        if (order == null)
        {
            return NotFound();
        }
        
        order.Status = OrderStatus.Accepted;
        service.SelectedClientId = order.ClientId;

        foreach (var otherOrder in service.Orders.Where(o => o.Id != orderId))
        {
            otherOrder.Status = OrderStatus.Rejected;
        }
        
        await _context.SaveChangesAsync();

        TempData["Success"] = "Заказ выбран. Остальные заказы отклонены.";
        return RedirectToAction(nameof(Details), new { id = order.ServiceId });
    }

    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectOrder(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.Service)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return NotFound();
        }

        if (order.Service.FreelancerId != _userManager.GetUserId(User))
        {
            return Forbid();
        }

        order.Status = OrderStatus.Rejected;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Заказ отклонен";
        return RedirectToAction(nameof(Details), new { id = order.ServiceId });
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
        
        order.Status = OrderStatus.Completed;
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Заказ выполнен.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }

    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> CancelService(int id)
    {
        var userId = _userManager.GetUserId(User);
        var service = await _context.Services
            .Include(s => s.Orders)
            .FirstOrDefaultAsync(s => s.Id == id && s.FreelancerId == userId);
        
        if (service == null)
        {
            return NotFound();
        }
        
        service.Status = ServiceStatus.Unavailable;
        foreach (var order in service.Orders)
        {
            order.Status = OrderStatus.Rejected;
        }
        
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Услуга отменена.";
        string? referer = Request.Headers["Referer"].ToString();
        return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("MyServices");
    }

    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> ResumeService(int id)
    {
        var userId = _userManager.GetUserId(User);
        var service = await _context.Services
            .Include(s => s.Orders)
            .FirstOrDefaultAsync(s => s.Id == id && s.FreelancerId == userId);
        
        if (service == null)
        {
            return NotFound();
        }
        
        if (service.Status != ServiceStatus.Unavailable)
        {
            return BadRequest("Услуга не находится в статусе 'Отменена'.");
        }

        service.Status = ServiceStatus.Available;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Услуга успешно возобновлёна.";
        string? referer = Request.Headers["Referer"].ToString();
        return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("MyServices");
    }
}