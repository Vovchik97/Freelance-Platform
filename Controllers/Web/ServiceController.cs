using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Api;
using FreelancePlatform.Dto.Categories;
using FreelancePlatform.Dto.Projects;
using FreelancePlatform.Dto.Services;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

public class ServiceController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly CategorySuggestionService _categorySuggestionService;
    private readonly RecommendationService _recommendationService;

    public ServiceController(
        AppDbContext context, 
        UserManager<IdentityUser> userManager, 
        CategorySuggestionService categorySuggestionService,
        RecommendationService recommendationService)
    {
        _context = context;
        _userManager = userManager;
        _categorySuggestionService = categorySuggestionService;
        _recommendationService = recommendationService;
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? search, 
        string? status, 
        decimal? minPrice, 
        decimal? maxPrice, 
        string sort,
        [FromQuery] List<int>? categories)
    {
        var query = _context.Services
            .Include(s => s.Freelancer)
            .Include(s => s.Reviews)
            .Include(s => s.Categories)
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

        // Фильтр по категориям
        if (categories != null && categories.Any())
        {
            query = query.Where(s => s.Categories.Any(c => categories.Contains(c.Id)));
        }
        
        if (sort == "price_desc")
            query = query.OrderByDescending(s => s.Price);
        else if (sort == "price_asc")
            query = query.OrderBy(s => s.Price);
        else
            query = query.OrderByDescending(s => s.CreatedAt);
        
        var services = await query.ToListAsync();

        // Все активные категории для фильтра
        ViewBag.AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.SelectedCategories = categories ?? new List<int>();

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.Sort = sort;
        
        if (User.Identity is { IsAuthenticated: true } && User.IsInRole("Client"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.Recommendations = await _recommendationService
                .GetRecommendedServicesForClientAsync(userId!);
        }
        
        return View(services);
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id, [FromQuery] List<int>? ratings)
    {
        var service = await _context.Services
            .Include(s => s.Freelancer)
            .Include(s => s.Categories)
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
    public async Task<IActionResult> Create()
    {
        ViewBag.AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.SelectedCategoryIds = new List<int>();
        return View();
    }
    
    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateServiceDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AllCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.SelectedCategoryIds = dto.CategoryIds;
            return View(dto);
        }
        
        var freelancerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (freelancerId == null)
        {
            return Unauthorized();
        }

        if (dto.CategoryIds == null || !dto.CategoryIds.Any())
        {
            dto.CategoryIds = await _categorySuggestionService.SuggestCategoryIdsAsync(dto.Title, dto.Description);
        }

        var selectedCategories = await _context.Categories
            .Where(c => dto.CategoryIds.Contains(c.Id) && c.IsActive)
            .ToListAsync();

        var service = new Service
        {
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            Status = dto.Status,
            FreelancerId = freelancerId,
            CreatedAt = DateTime.UtcNow,
            Categories = selectedCategories
        };
        
        await _context.Services.AddAsync(service);
        await _context.SaveChangesAsync();
        ViewBag.SelectedCategoryIds = dto.CategoryIds;
        return RedirectToAction(nameof(MyServices));
    }
    
    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var service = await _context.Services
            .Include(s => s.Categories)
            .FirstOrDefaultAsync(s => s.Id == id && s.FreelancerId == userId);
        
        if (service == null)
        {
            return NotFound();
        }

        var dto = new UpdateServiceDto
        {
            Title = service.Title,
            Description = service.Description,
            Price = service.Price,
            Status = service.Status,
            CategoryIds = service.Categories.Select(c => c.Id).ToList()
        };
        
        ViewBag.AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        
        ViewBag.SelectedCategoryIds = dto.CategoryIds;
        
        return View(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateServiceDto dto)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AllCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return View(dto);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var service = await _context.Services
            .Include(s => s.Categories)
            .FirstOrDefaultAsync(s => s.Id == id && s.FreelancerId == userId);

        if (service == null)
        {
            return NotFound();
        }
        
        service.Title = dto.Title;
        service.Description = dto.Description;
        service.Price = dto.Price;
        service.Status = dto.Status;

        if (dto.CategoryIds == null || !dto.CategoryIds.Any())
        {
            dto.CategoryIds = await _categorySuggestionService.SuggestCategoryIdsAsync(dto.Title, dto.Description);
        }

        // Обновление категорий
        service.Categories.Clear();
        var selectedCategories = await _context.Categories
            .Where(c => dto.CategoryIds.Contains(c.Id) && c.IsActive)
            .ToListAsync();
        foreach (var cat in selectedCategories)
        {
            service.Categories.Add(cat);
        }

        await _context.SaveChangesAsync();
        ViewBag.SelectedCategoryIds = dto.CategoryIds;
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
            .Include(s => s.Categories)
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

    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SuggestCategories([FromBody] SuggestCategoriesRequest request)
    {
        if (request == null)
        {
            return Json(new List<int>());
        }

        var suggestedIds = await _categorySuggestionService.SuggestCategoryIdsAsync(request.Title, request.Description);
        return Json(suggestedIds);
    }
}