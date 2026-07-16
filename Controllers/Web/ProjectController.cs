using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Dto.Categories;
using FreelancePlatform.Dto.Projects;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

/// <summary>
/// Контроллер управления проектами.
/// Позволяет создавать, редактировать и удалять проекты,
/// обрабатывать заявки исполнителей, управлять статусами проектов,
/// выполнять финансовые операции и формировать рекомендации.
/// </summary>
public class ProjectController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly BalanceService _balanceService;
    private readonly CategorySuggestionService _categorySuggestionService;
    private readonly RecommendationService _recommendationService;
    private readonly WorkItemService _workItemService;
    private readonly IReputationService _reputationService;
    private readonly IBlacklistService _blacklistService;

    public ProjectController(AppDbContext context, 
        UserManager<IdentityUser> userManager, 
        BalanceService balanceService, 
        CategorySuggestionService categorySuggestionService,
        RecommendationService recommendationService,
        WorkItemService workItemService,
        IReputationService reputationService,
        IBlacklistService blacklistService)
    {
        _context = context;
        _userManager = userManager;
        _balanceService = balanceService;
        _categorySuggestionService = categorySuggestionService;
        _recommendationService = recommendationService;
        _workItemService = workItemService;
        _reputationService = reputationService;
        _blacklistService = blacklistService;
    }
    
    /// <summary>
    /// Отображает список доступных проектов.
    /// Поддерживает поиск, фильтрацию по статусу, бюджету, категориям и типу проекта.
    /// Для авторизованных исполнителей загружает персональные рекомендации.
    /// </summary>
    /// <param name="search">Строка поиска по названию и описанию проекта.</param>
    /// <param name="status">Статус проекта для фильтрации.</param>
    /// <param name="minBudget">Минимальный бюджет проекта.</param>
    /// <param name="maxBudget">Максимальный бюджет проекта.</param>
    /// <param name="sort">Параметр сортировки проектов.</param>
    /// <param name="categories">Список идентификаторов категорий.</param>
    /// <param name="projectType">Тип проекта: командный или индивидуальный.</param>
    /// <returns>Страница со списком проектов.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? search, 
        string? status, 
        decimal? minBudget, 
        decimal? maxBudget, 
        string sort,
        [FromQuery] List<int>? categories,
        string? projectType)
    {
        var query = _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Categories)
            .Include(p => p.Members)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Title.Contains(search) || p.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProjectStatus>(status, out var parsedStatus))
        {
            query = query.Where(p => p.Status == parsedStatus);
        }

        if (minBudget.HasValue)
        {
            query = query.Where(p => p.Budget >= minBudget);    
        }

        if (maxBudget.HasValue)
        {
            query = query.Where(p => p.Budget <= maxBudget);
        }
        
        if (categories != null && categories.Any())
        {
            query = query.Where(p => p.Categories.Any(c => categories.Contains(c.Id)));
        }

        if (projectType == "team")
        {
            query = query.Where(p => p.IsTeamProject);
        }
        else if (projectType == "solo")
        {
            query = query.Where(p => !p.IsTeamProject);
        }
        
        if (sort == "budget_desc")
            query = query.OrderByDescending(p => p.Budget);
        else if (sort == "budget_asc")
            query = query.OrderBy(p => p.Budget);
        else
            query = query.OrderByDescending(p => p.CreatedAt);
        
        var projects = await query.ToListAsync();
        
        var currentUserId = _userManager.GetUserId(User);
        if (currentUserId != null)
        {
            var blockedUserIds = await _context.BlacklistEntries
                .Where(b => b.BlockerId == currentUserId || b.BlockedId == currentUserId)
                .Select(b => b.BlockerId == currentUserId ? b.BlockedId : b.BlockerId)
                .Distinct()
                .ToListAsync();

            projects = projects
                .Where(p => !blockedUserIds.Contains(p.ClientId))
                .ToList();
        }

        await LoadAllCategoriesAsync();
        ViewBag.SelectedCategories = categories ?? new List<int>();

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.MinBudget = minBudget;
        ViewBag.MaxBudget = maxBudget;
        ViewBag.Sort = sort;
        ViewBag.ProjectType = projectType;

        if (User.Identity is { IsAuthenticated: true } && User.IsInRole("Freelancer"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.Recommendations = await _recommendationService
                .GetRecommendedProjectsForFreelancerAsync(userId!);
        }
        
        return View(projects);
    }
    
    /// <summary>
    /// Отображает подробную информацию о проекте.
    /// Загружает клиента, категории, заявки исполнителей, рабочие элементы и прогресс выполнения.
    /// </summary>
    /// <param name="id">Идентификатор проекта.</param>
    /// <returns>Страница проекта или 404, если проект не найден.</returns>
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Categories)
            .Include(p => p.SelectedFreelancer)
            .Include(p => p.Bids)
                .ThenInclude(b => b.Freelancer)
            .Include(p => p.WorkItems)
                .ThenInclude(w => w.CreatedBy)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }
        
        var progress = await _workItemService.GetProgressAsync(id, null); 
        
        ViewBag.Progress = progress;
        ViewBag.WorkItems = project.WorkItems.OrderBy(w => w.OrderIndex).ToList();
        ViewBag.TaskTemplates = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .ToListAsync();
        
        return View(project);
    }

    /// <summary>
    /// Отображает форму создания проекта.
    /// Загружает категории и шаблоны задач, доступные для выбора пользователю.
    /// </summary>
    /// <returns>Страница создания проекта.</returns>
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Create()
    {
        await LoadAllCategoriesAsync();
        ViewBag.SelectedCategoryIds = new List<int>();
        ViewBag.TaskTemplates = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .ToListAsync();
        
        var model = new CreateProjectDto 
        { 
            IsTeamProject = false,
            Status = ProjectStatus.Open,
            CategoryIds = new List<int>()
        };
        
        return View(model);
    }
    
    /// <summary>
    /// Создаёт новый проект клиента.
    /// Назначает категории проекта, автоматически предлагает категории при необходимости
    /// и создаёт задачи из выбранного шаблона.
    /// </summary>
    /// <param name="dto">Данные создаваемого проекта.</param>
    /// <param name="templateId">Идентификатор шаблона задач.</param>
    /// <returns>Перенаправление к списку проектов.</returns>
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProjectDto dto, int? templateId = null)
    {
        if (!ModelState.IsValid)
        {
            await LoadAllCategoriesAsync();
            ViewBag.SelectedCategoryIds = dto.CategoryIds;
            ViewBag.TaskTemplates = await _context.TaskTemplates.ToListAsync();
            return View(dto);
        }
        
        var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (clientId == null)
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

        var project = new Project
        {
            Title = dto.Title,
            Description = dto.Description,
            Budget = dto.Budget,
            Status = dto.Status,
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            Categories = selectedCategories,
            IsTeamProject = dto.IsTeamProject
        };
        
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        ViewBag.SelectedCategoryIds = dto.CategoryIds;

        if (templateId.HasValue && templateId > 0)
        {
            await _workItemService.CreateFromTemplateAsync(templateId.Value, project.Id, null, clientId);
        }
        
        return RedirectToAction(nameof(MyProjects));
    }
    
    /// <summary>
    /// Отображает форму редактирования проекта.
    /// Загружает текущие данные проекта и выбранные категории.
    /// </summary>
    /// <param name="id">Идентификатор проекта.</param>
    /// <returns>Форма редактирования или 404.</returns>
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);
        
        if (project == null)
        {
            return NotFound();
        }

        var dto = new UpdateProjectDto
        {
            Title = project.Title,
            Description = project.Description,
            Budget = project.Budget,
            Status = project.Status,
            CategoryIds = project.Categories.Select(c => c.Id).ToList()
        };

        await LoadAllCategoriesAsync();
        ViewBag.SelectedCategoryIds = dto.CategoryIds; 
        
        return View(dto);
    }

    /// <summary>
    /// Сохраняет изменения проекта.
    /// Обновляет основные данные и категории проекта.
    /// При отсутствии категорий выполняет автоматический подбор.
    /// </summary>
    /// <param name="id">Идентификатор проекта.</param>
    /// <param name="dto">Обновленные данные проекта.</param>
    /// <returns>Перенаправление к проектам пользователя.</returns>
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateProjectDto dto)
    {
        if (!ModelState.IsValid)
        {
            await LoadAllCategoriesAsync();
            return View(dto);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }
        
        project.Title = dto.Title;
        project.Description = dto.Description;
        project.Budget = dto.Budget;
        project.Status = dto.Status;

        if (dto.CategoryIds == null || !dto.CategoryIds.Any())
        {
            dto.CategoryIds = await _categorySuggestionService.SuggestCategoryIdsAsync(dto.Title, dto.Description);
        }
        
        project.Categories.Clear();
        var selectedCategories = await _context.Categories
            .Where(c => dto.CategoryIds.Contains(c.Id) && c.IsActive)
            .ToListAsync();
        foreach (var cat in selectedCategories)
        {
            project.Categories.Add(cat);
        }

        await _context.SaveChangesAsync();
        ViewBag.SelectedCategoryIds = dto.CategoryIds;
        return RedirectToAction(nameof(MyProjects));
    }

    /// <summary>
    /// Удаляет проект клиента вместе со связанными рабочими элементами.
    /// </summary>
    /// <param name="id">Идентификатор проекта.</param>
    /// <returns>Перенаправление к списку проектов.</returns>
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects
            .Include(p => p.WorkItems)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }

        if (project.WorkItems != null && project.WorkItems.Any())
        {
            _context.WorkItems.RemoveRange(project.WorkItems);
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(MyProjects));
    }

    /// <summary>
    /// Отображает проекты текущего клиента.
    /// Также загружает количество непрочитанных сообщений в командных проектах.
    /// </summary>
    /// <returns>Страница проектов пользователя.</returns>
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> MyProjects()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var myProjects = await _context.Projects
            .Include(p => p.Categories)
            .Include(p => p.Members)
            .Where(p => p.ClientId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var teamProjects = myProjects.Where(p => p.IsTeamProject).ToList();
        var unreadByProject = new Dictionary<int, int>();

        foreach (var project in teamProjects)
        {
            var count = await _context.GroupChatMessages
                .Where(m => m.ProjectId == project.Id && m.SenderId != userId && m.ParentMessageId == null &&
                            !m.ReadBy.Any(r => r.UserId == userId))
                .CountAsync();

            unreadByProject[project.Id] = count;
        }
        
        ViewBag.UnreadByProject = unreadByProject;

        return View(myProjects);
    }

    /// <summary>
    /// Принимает заявку исполнителя на проект.
    /// Назначает исполнителя, отклоняет остальные заявки и создаёт чат между клиентом и исполнителем.
    /// </summary>
    /// <param name="projectId">Идентификатор проекта.</param>
    /// <param name="bidId">Идентификатор принятой заявки.</param>
    /// <returns>Страница проекта.</returns>
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptBid(int projectId, int bidId)
    {
        var project = await _context.Projects
            .Include(p => p.Bids)
            .FirstOrDefaultAsync(p => p.Id == projectId);
        
        if (project == null)
        {
            return NotFound();
        }

        if (project.ClientId != _userManager.GetUserId(User))
        {
            return Forbid();
        }

        var bid = project.Bids.FirstOrDefault(b => b.Id == bidId);
        if (bid == null)
        {
            return NotFound();
        }
        
        var isBlocked = await _blacklistService.IsBlockedEitherWayAsync(
            project.ClientId, bid.FreelancerId);

        if (isBlocked)
        {
            TempData["ErrorMessage"] = "Вы не можете принять эту заявку — один из вас заблокировал другого.";
            return RedirectToAction("Details", new { id = projectId });
        }
        
        bid.Status = BidStatus.Accepted;
        project.SelectedFreelancerId = bid.FreelancerId;
        project.Status = ProjectStatus.InProgress;

        foreach (var otherBid in project.Bids.Where(b => b.Id != bidId))
        {
            otherBid.Status = BidStatus.Rejected;
        }
        
        var existingChat = await _context.Chats
            .FirstOrDefaultAsync(c => c.ClientId == project.ClientId && c.FreelancerId == bid.FreelancerId);
        
        if (existingChat == null)
        {
            var chat = new Chat
            {
                ClientId = project.ClientId,
                FreelancerId = bid.FreelancerId,
                Messages = new List<Message>()
            };
            
            _context.Chats.Add(chat);
        }
        
        await _context.SaveChangesAsync();

        TempData["Success"] = "Исполнитель выбран. Остальные заявки отклонены.";
        return RedirectToAction(nameof(Details), new { id = bid.ProjectId });
    }

    /// <summary>
    /// Отклоняет заявку исполнителя на проект.
    /// </summary>
    /// <param name="bidId">Идентификатор заявки.</param>
    /// <returns>Страница проекта.</returns>
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBid(int bidId)
    {
        var bid = await _context.Bids
            .FirstOrDefaultAsync(b => b.Id == bidId);

        if (bid == null)
        {
            return NotFound();
        }
        
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == bid.ProjectId);

        if (project == null)
        {
            return NotFound();
        }

        if (project.ClientId != _userManager.GetUserId(User))
        {
            return Forbid();
        }

        bid.Status = BidStatus.Rejected;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Заявка отклонена";
        return RedirectToAction(nameof(Details), new { id = bid.ProjectId });
    }

    /// <summary>
    /// Завершает выполнение проекта.
    /// Освобождает замороженные средства, изменяет статус проекта и начисляет репутацию исполнителю.
    /// </summary>
    /// <param name="id">Идентификатор проекта.</param>
    /// <returns>Страница проекта.</returns>
    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> CompleteProject(int id)
    {
        var userId = _userManager.GetUserId(User);
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
        
        if (project == null)
        {
            return NotFound();
        }
        
        var acceptBid = await _context.Bids.FirstOrDefaultAsync(b => b.ProjectId == project.Id && b.Status == BidStatus.Accepted);

        if (project.SelectedFreelancerId != userId)
        {
            return Forbid();
        }

        if (project.Status != ProjectStatus.Paid)
        {
            return BadRequest("Проект не может быть завершён.");
        }

        if (acceptBid == null)
        {
            return BadRequest("Принятая заявка не найдена.");
        }

        await _balanceService.ReleaseForProjectAsync(
            project.ClientId,
            project.SelectedFreelancerId,
            acceptBid.Amount,
            project.Id
        );

        project.Status = ProjectStatus.Completed;
        await _context.SaveChangesAsync();

        await ApplyDeliveryReputationAsync(userId, acceptBid.CreatedAt, acceptBid.DurationInDays,
            projectId: project.Id);
        
        TempData["SuccessMessage"] = "Проект завершён.";
        return RedirectToAction(nameof(Details), new { id = project.Id });
    }

    /// <summary>
    /// Отменяет проект клиента.
    /// Возвращает замороженные средства, переводит заявки в отклонённое состояние.
    /// </summary>
    /// <param name="id">Идентификатор проекта.</param>
    /// <returns>Перенаправление обратно к проектам.</returns>
    [HttpPost]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> CancelProject(int id)
    {
        var userId = _userManager.GetUserId(User);
        var project = await _context.Projects
            .Include(p => p.Bids)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);
        
        if (project == null)
        {
            return NotFound();
        }

        if (project.Status == ProjectStatus.InProgress)
        {
            await _balanceService.RefundForProjectAsync(
                project.ClientId,
                project.Budget,
                project.Id
            );
        }

        if (project.Status != ProjectStatus.Open && project.Status != ProjectStatus.InProgress)
        {
            return BadRequest("Проект нельзя отменить на текущей стадии.");
        }

        project.Status = ProjectStatus.Cancelled;
        foreach (var bid in project.Bids)
        {
            bid.Status = BidStatus.Rejected;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Проект отменен.";
        string? referer = Request.Headers["Referer"].ToString();
        return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("MyProjects");
    }

    /// <summary>
    /// Возобновляет отменённый проект.
    /// Переводит его обратно в состояние ожидания заявок.
    /// </summary>
    /// <param name="id">Идентификатор проекта.</param>
    /// <returns>Перенаправление к проектам.</returns>
    [HttpPost]
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> ResumeProject(int id)
    {
        var userId = _userManager.GetUserId(User);
        var project = await _context.Projects
            .Include(p => p.Bids)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);
        
        if (project == null)
        {
            return NotFound();
        }

        if (project.Status != ProjectStatus.Cancelled)
        {
            return BadRequest("Проект не находится в статусе 'Отменён'.");
        }

        project.Status = ProjectStatus.Open;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Проект успешно возобновлён.";
        string? referer = Request.Headers["Referer"].ToString();
        return !string.IsNullOrEmpty(referer) ? Redirect(referer) : RedirectToAction("MyProjects");
    }

    /// <summary>
    /// Возвращает автоматически предложенные категории для проекта на основе его описания.
    /// </summary>
    /// <param name="request">Данные проекта для анализа.</param>
    /// <returns>Список идентификаторов подходящих категорий.</returns>
    [HttpPost]
    [Authorize(Roles = "Client")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> SuggestCategories([FromBody] SuggestCategoriesRequest request)
    {
        if (request  == null)
        {
            return Json(new List<int>());
        }
        
        var suggestedIds = await _categorySuggestionService.SuggestCategoryIdsAsync(request.Title, request.Description);
        return Json(suggestedIds);
    }

    /// <summary>
    /// Рассчитывает событие репутации исполнителя
    /// в зависимости от соблюдения срока выполнения проекта.
    /// </summary>
    /// <param name="freelancerId">Идентификатор исполнителя.</param>
    /// <param name="startedAt">Дата начала выполнения.</param>
    /// <param name="durationInDays">Срок выполнения в днях.</param>
    /// <param name="orderId">Идентификатор заказа.</param>
    /// <param name="projectId">Идентификатор проекта.</param>
    private async Task ApplyDeliveryReputationAsync(
        string freelancerId,
        DateTime startedAt,
        int durationInDays,
        int? orderId = null,
        int? projectId = null)
    {
        var now = DateTime.UtcNow;
        var deadline = startedAt.AddDays(durationInDays);
        
        ReputationEventType eventType;
        string reason;
        
        if (durationInDays <= 0)
        {
            eventType = ReputationEventType.OnTimeDelivery;
            reason = "Работа выполнена";
        }
        else if (now < deadline.AddDays(-1))
        {
            eventType = ReputationEventType.EarlyDelivery;
            reason = $"Досрочная сдача (срок {durationInDays} дн., дедлайн {deadline:dd.MM.yyyy})";
        }
        else if (now <= deadline)
        {
            eventType = ReputationEventType.OnTimeDelivery;
            reason = $"Сдача в срок (срок {durationInDays} дн., дедлайн {deadline:dd.MM.yyyy})";
        }
        else
        {
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
    /// Загружает все категории.
    /// </summary>
    private async Task LoadAllCategoriesAsync()
    {
        ViewBag.AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}