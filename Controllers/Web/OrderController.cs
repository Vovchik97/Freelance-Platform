using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Dto;
using FreelancePlatform.Dto.Bids;
using FreelancePlatform.Dto.Orders;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

public class OrderController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public OrderController(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Client)
            .Include(o => o.Service)
                .ThenInclude(s => s!.Freelancer)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        return View(order);
    }

    [Authorize(Roles = "Client")]
    public IActionResult Create(int serviceId)
    {
        var dto = new CreateOrderDto
        {
            ServiceId = serviceId
        };
        ViewBag.ServiceId = serviceId;
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateOrderDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var service = await _context.Services.FirstOrDefaultAsync(s => s.Id == dto.ServiceId);

        var hasActiveOrder = service?.Orders?.Any(o => o.Client!.Id == userId && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Accepted)) ?? false;

        if (hasActiveOrder)
        {
            ModelState.AddModelError(string.Empty, "Вы уже отправили заказ на эту услугу.");
            ViewBag.ProjectId = dto.ServiceId;
            return View(dto);
        }
        
        var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (clientId == null)
        {
            return Unauthorized();
        }
        
        var order = new Order
        {
            Comment = dto.Comment,
            DurationInDays = dto.DurationInDays,
            CreatedAt = DateTime.UtcNow,
            ClientId = clientId,
            ServiceId = dto.ServiceId
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

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
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id && o.ClientId == userId);

        if (order == null)
        {
            return NotFound();
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
        
        order.Status = OrderStatus.Completed;
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Заказ выполнен.";
        return RedirectToAction(nameof(Details), new { id = order.Id });
    }
}