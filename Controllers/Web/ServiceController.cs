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
using Xunit.Sdk;

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
    public async Task<IActionResult> Index(string? search, string? status, decimal? minPrice, decimal? maxPrice, string sort)
    {
        var query = _context.Services
            .Include(s => s.Freelancer)
            .Include(s => s.Reviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Title.Contains(search) || s.Description.Contains(search));
        }
        
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ServiceStatus>(status, out var parsedStatus))
        {
            query = query.Where(s => s.Status == parsedStatus);
        }
        
        if (minPrice.HasValue)
        {
            query = query.Where(s => s.Price >= minPrice);    
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(s => s.Price <= maxPrice);
        }
        
        if (sort == "price_desc")
            query = query.OrderByDescending(s => s.Price);
        else if (sort == "price_asc")
            query = query.OrderBy(s => s.Price);
        else
            query = query.OrderByDescending(s => s.CreatedAt);
        
        var services = await query.ToListAsync();

        ViewBag.Searcg = search;
        ViewBag.Status = status;
        ViewBag.MinBudget = minPrice;
        ViewBag.MaxBudget = maxPrice;
        ViewBag.Sort = sort;
        
        return View(services);
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id, [FromQuery] List<int>? ratings)
    {
        var service = await _context.Services
            .Include(s => s.Freelancer)
            .Include(s => s.Reviews)
                .ThenInclude(r => r.User)
            .Include(s => s.Orders)
                .ThenInclude(o => o.Client)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (service == null)
        {
            return NotFound();
        }

        var allReviews = service.Reviews?.ToList() ?? new List<Review>();

        var averageRating = allReviews.Count > 0 ? allReviews.Average(r => r.Rating) : 0.0;
        var reviewsCount = allReviews.Count;

        var counts = Enumerable.Range(1, 5)
            .ToDictionary(star => star, star => allReviews.Count(r => r.Rating == star));

        var selected = (ratings ?? new List<int>())
            .Where(star => star >= 1 && star <= 5)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        var filtered = selected.Any()
            ? allReviews.Where(r => selected.Contains(r.Rating))
            : allReviews;
        
        service.Reviews = filtered
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        
        ViewBag.AverageRating = averageRating;
        ViewBag.ReviewsCount = reviewsCount;
        ViewBag.ReviewCounts = counts;
        ViewBag.SelectedRatings = selected;
        
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
        
        var freelancerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (freelancerId == null)
        {
            return Unauthorized();
        }

        var service = new Service
        {
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            Status = dto.Status,
            FreelancerId = freelancerId,
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
            .Include(s => s.Reviews)
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
            if (otherOrder.Status != OrderStatus.Completed)
            {
                otherOrder.Status = OrderStatus.Rejected;
            }
        }
        
        var existingChat = await _context.Chats
            .FirstOrDefaultAsync(c => c.FreelancerId == service.FreelancerId && c.ClientId == order.ClientId);

        if (existingChat == null)
        {
            var chat = new Chat
            {
                ClientId = order.ClientId,
                FreelancerId = service.FreelancerId,
                Messages = new List<Message>()
            };
        
            _context.Chats.Add(chat);
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

        if (order.Service?.FreelancerId != _userManager.GetUserId(User))
        {
            return Forbid();
        }

        order.Status = OrderStatus.Rejected;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Заказ отклонен";
        return RedirectToAction(nameof(Details), new { id = order.ServiceId });
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

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(int serviceId, string content, int rating)
    {
        var userId = _userManager.GetUserId(User);

        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] = "Оценка должна быть от 1 до 5.";
            return RedirectToAction("Details", new { id = serviceId });
        }

        var hasOrdered = await _context.Orders
            .AnyAsync(o => o.ServiceId == serviceId &&
                           o.ClientId == userId &&
                           o.Status == OrderStatus.Completed);

        if (!hasOrdered)
        {
            TempData["ErrorMessage"] = "Оставить отзыв можно только после выполнения заказа.";
            return RedirectToAction("Details", new { id = serviceId });
        }

        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ServiceId == serviceId && r.UserId == userId);

        if (existingReview != null)
        {
            existingReview.Rating = rating;
            existingReview.Comment = content;
            existingReview.CreatedAt = DateTime.UtcNow;

            _context.Reviews.Update(existingReview);
        }
        else
        {
            var review = new Review
            {
                ServiceId = serviceId,
                UserId = userId!,
                Rating = rating,
                Comment = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
        }
        
        await _context.SaveChangesAsync();
        
        return RedirectToAction("Details", new { id = serviceId });
    }
}