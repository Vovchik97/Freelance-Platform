using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Dto.Categories;
using FreelancePlatform.Dto.Reviews;
using FreelancePlatform.Dto.Services;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

/// <summary>
/// Контроллер управления услугами исполнителей.
/// Предоставляет функциональность просмотра, создания,
/// редактирования и удаления услуг, обработки заказов,
/// отзывов и рекомендаций.
/// </summary>
public class ServiceController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly CategorySuggestionService _categorySuggestionService;
    private readonly RecommendationService _recommendationService;
    private readonly IReputationService _reputationService;
    private readonly IBlacklistService _blacklistService;

    public ServiceController(
        AppDbContext context, 
        UserManager<IdentityUser> userManager, 
        CategorySuggestionService categorySuggestionService,
        RecommendationService recommendationService,
        IReputationService reputationService,
        IBlacklistService blacklistService)
    {
        _context = context;
        _userManager = userManager;
        _categorySuggestionService = categorySuggestionService;
        _recommendationService = recommendationService;
        _reputationService = reputationService;
        _blacklistService = blacklistService;
    }
    
    /// <summary>
    /// Отображает каталог услуг с возможностью поиска,
    /// фильтрации, сортировки и получения рекомендаций.
    /// </summary>
    /// <param name="search">Строка поиска по названию или описанию услуги.</param>
    /// <param name="status">Статус услуги.</param>
    /// <param name="minPrice">Минимальная стоимость.</param>
    /// <param name="maxPrice">Максимальная стоимость.</param>
    /// <param name="sort">Способ сортировки списка.</param>
    /// <param name="categories">Список идентификаторов выбранных категорий.</param>
    /// <returns>Страница со списком услуг.</returns>
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

        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId != null)
        {
            var blockedUserIds = await _context.BlacklistEntries
                .Where(b => b.BlockerId == currentUserId || b.BlockedId == currentUserId)
                .Select(b => b.BlockerId == currentUserId ? b.BlockedId : b.BlockerId)
                .Distinct()
                .ToListAsync();

            services = services
                .Where(s => !blockedUserIds.Contains(s.FreelancerId))
                .ToList();
        }
        
        await LoadAllCategoriesAsync();
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
    
    /// <summary>
    /// Отображает подробную информацию об услуге,
    /// включая отзывы, статистику оценок и возможность
    /// оставить отзыв после завершённого заказа.
    /// </summary>
    /// <param name="id">Идентификатор услуги.</param>
    /// <param name="ratings">Фильтр отзывов по оценкам.</param>
    /// <returns>Страница выбранной услуги.</returns>
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

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        bool canReview = false;
        if (currentUserId != null && User.IsInRole("Client"))
        {
            canReview = await _context.Orders
                .AnyAsync(o => o.ServiceId == id
                               && o.ClientId == currentUserId
                               && o.Status == OrderStatus.Completed);
        }

        var selected = (ratings ?? new List<int>())
            .Where(s => s >= 1 && s <= 5).Distinct()
            .OrderByDescending(x => x).ToList();

        service.Reviews = (selected.Any()
                ? allReviews.Where(r => selected.Contains(r.Rating))
                : allReviews)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
        
        var hasReviews = allReviews.Any();

        ViewBag.AvgQuality = hasReviews ? allReviews.Average(r => r.QualityRating) : 0.0;
        ViewBag.AvgCommunication = hasReviews ? allReviews.Average(r => r.CommunicationRating) : 0.0;
        ViewBag.AvgDeadline = hasReviews ? allReviews.Average(r => r.DeadlineRating) : 0.0;
        ViewBag.AvgPrice = hasReviews ? allReviews.Average(r => r.PriceRating) : 0.0;
        
        ViewBag.CanReview = canReview;
        ViewBag.AverageRating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0.0;
        ViewBag.ReviewsCount = allReviews.Count;
        ViewBag.ReviewCounts = Enumerable.Range(1, 5)
            .ToDictionary(s => s, s => allReviews.Count(r => r.Rating == s));
        
        ViewBag.SelectedRatings = selected;
        
        return View(service);
    }

    /// <summary>
    /// Отображает форму создания новой услуги.
    /// </summary>
    /// <returns>Страница создания услуги.</returns>
    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> Create()
    {
        await LoadAllCategoriesAsync();
        ViewBag.SelectedCategoryIds = new List<int>();
        return View();
    }
    
    /// <summary>
    /// Создаёт новую услугу исполнителя.
    /// При отсутствии выбранных категорий автоматически определяет наиболее подходящие.
    /// </summary>
    /// <param name="dto">Данные создаваемой услуги.</param>
    /// <returns>Перенаправление к списку услуг либо повторное отображение формы.</returns>
    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateServiceDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadAllCategoriesAsync();
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
    
    /// <summary>
    /// Отображает форму редактирования услуги.
    /// </summary>
    /// <param name="id">Идентификатор услуги.</param>
    /// <returns>Страница редактирования услуги.</returns>
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
        
        await LoadAllCategoriesAsync();
        
        ViewBag.SelectedCategoryIds = dto.CategoryIds;
        
        return View(dto);
    }

    /// <summary>
    /// Сохраняет изменения информации об услуге.
    /// При отсутствии категорий автоматически подбирает наиболее подходящие.
    /// </summary>
    /// <param name="id">Идентификатор услуги.</param>
    /// <param name="dto">Обновлённые данные услуги.</param>
    /// <returns>Перенаправление к списку услуг либо повторное отображение формы.</returns>
    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateServiceDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadAllCategoriesAsync();
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

    /// <summary>
    /// Удаляет услугу текущего исполнителя.
    /// </summary>
    /// <param name="id">Идентификатор услуги.</param>
    /// <returns>Перенаправление к списку услуг пользователя.</returns>
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

    /// <summary>
    /// Отображает список услуг текущего исполнителя.
    /// </summary>
    /// <returns>Страница со списком собственных услуг.</returns>
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

    /// <summary>
    /// Принимает выбранный заказ, отклоняет остальные активные заказы
    /// и при необходимости создаёт чат между сторонами.
    /// </summary>
    /// <param name="serviceId">Идентификатор услуги.</param>
    /// <param name="orderId">Идентификатор заказа.</param>
    /// <returns>Перенаправление на страницу услуги.</returns>
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
        
        var isBlocked = await _blacklistService.IsBlockedEitherWayAsync(
            service.FreelancerId, order.ClientId);

        if (isBlocked)
        {
            TempData["ErrorMessage"] = "Вы не можете принять этот заказ — один из вас заблокировал другого.";
            return RedirectToAction("Details", new { id = serviceId });
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

    /// <summary>
    /// Отклоняет выбранный заказ.
    /// </summary>
    /// <param name="orderId">Идентификатор заказа.</param>
    /// <returns>Перенаправление на страницу услуги.</returns>
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

    /// <summary>
    /// Переводит услугу в статус недоступной и отклоняет все связанные заказы.
    /// </summary>
    /// <param name="id">Идентификатор услуги.</param>
    /// <returns>Перенаправление на предыдущую страницу либо к списку услуг.</returns>
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

    /// <summary>
    /// Возобновляет ранее отключённую услугу.
    /// </summary>
    /// <param name="id">Идентификатор услуги.</param>
    /// <returns>Перенаправление на предыдущую страницу либо к списку услуг.</returns>
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

    /// <summary>
    /// Добавляет новый отзыв либо обновляет существующий,
    /// а также пересчитывает репутацию исполнителя.
    /// </summary>
    /// <param name="dto">Данные отзыва.</param>
    /// <returns>Перенаправление на страницу услуги.</returns>
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddReview(AddReviewDto dto)
    {
        var userId = _userManager.GetUserId(User);

        var hasOrdered = await _context.Orders
            .AnyAsync(o => o.ServiceId == dto.ServiceId &&
                           o.ClientId == userId &&
                           o.Status == OrderStatus.Completed);

        if (!hasOrdered)
        {
            TempData["ErrorMessage"] = "Оставить отзыв можно только после выполнения заказа.";
            return RedirectToAction("Details", new { id = dto.ServiceId });
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Проверьте правильность заполнения формы.";
            return RedirectToAction("Details", new { id = dto.ServiceId });
        }

        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId);

        var existing = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ServiceId == dto.ServiceId && r.UserId == userId);

        bool isNewReview = existing == null;
        
        if (existing != null)
        {
            var oldEventType = existing.Rating >= 4
                ? ReputationEventType.PositiveReview
                : ReputationEventType.NegativeReview;

            var reverseType = oldEventType == ReputationEventType.PositiveReview
                ? ReputationEventType.NegativeReview
                : ReputationEventType.PositiveReview;

            await _reputationService.AddEventAsync(
                service!.FreelancerId,
                reverseType,
                reason: "Откат предыдущего отзыва при редактировании"
            );
            
            existing.Rating = dto.Rating;
            existing.QualityRating = dto.QualityRating;
            existing.CommunicationRating = dto.CommunicationRating;
            existing.DeadlineRating = dto.DeadlineRating;
            existing.PriceRating = dto.PriceRating;
            existing.Comment = dto.Comment;
            existing.CreatedAt = DateTime.UtcNow;

            _context.Reviews.Update(existing);
        }
        else
        {
            var review = new Review
            {
                ServiceId = dto.ServiceId,
                UserId = userId!,
                Rating = dto.Rating,
                QualityRating = dto.QualityRating,
                CommunicationRating = dto.CommunicationRating,
                DeadlineRating = dto.DeadlineRating,
                PriceRating = dto.PriceRating,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
        }
        
        await _context.SaveChangesAsync();

        if (service != null && isNewReview)
        {
            var reputationEvent = dto.Rating >= 4
                ? ReputationEventType.PositiveReview
                : ReputationEventType.NegativeReview;
            
            var reason = dto.Rating >= 4
                ? $"Положительный отзыв (оценка {dto.Rating}/5)"
                : $"Отрицательный отзыв (оценка {dto.Rating}/5)";

            await _reputationService.AddEventAsync(
                service.FreelancerId,
                reputationEvent,
                reason: reason
            );
        }
        
        TempData["SuccessMessage"] = "Отзыв успешно сохранён!";
        return RedirectToAction("Details", new { id = dto.ServiceId });
    }

    /// <summary>
    /// Возвращает список рекомендуемых категорий
    /// на основании названия и описания услуги.
    /// </summary>
    /// <param name="request">Данные для определения категорий.</param>
    /// <returns>JSON со списком идентификаторов категорий.</returns>
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

    private async Task LoadAllCategoriesAsync()
    {
        ViewBag.AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}