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

public class ProjectController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly BalanceService _balanceService;
    private readonly CategorySuggestionService _categorySuggestionService;
    private readonly RecommendationService _recommendationService;

    public ProjectController(AppDbContext context, 
        UserManager<IdentityUser> userManager, 
        BalanceService balanceService, 
        CategorySuggestionService categorySuggestionService,
        RecommendationService recommendationService)
    {
        _context = context;
        _userManager = userManager;
        _balanceService = balanceService;
        _categorySuggestionService = categorySuggestionService;
        _recommendationService = recommendationService;
    }
    
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

        // Фильтр по категориям
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

        // Все активные категории для фильтра
        ViewBag.AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
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
    
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Categories)
            .Include(p => p.Bids)
                .ThenInclude(b => b.Freelancer)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }
        
        return View(project);
    }

    [Authorize(Roles = "Client")]
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
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProjectDto dto)
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
        return RedirectToAction(nameof(MyProjects));
    }
    
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
        
        ViewBag.AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.SelectedCategoryIds = dto.CategoryIds; 
        
        return View(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateProjectDto dto)
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

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(MyProjects));
    }

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

    [HttpPost]
    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> CompleteProject(int id)
    {
        var userId = _userManager.GetUserId(User);
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id);
        var acceptBid = await _context.Bids.FirstOrDefaultAsync(b => b.ProjectId == project.Id && b.Status == BidStatus.Accepted);
        
        if (project == null)
        {
            return NotFound();
        }

        if (project.SelectedFreelancerId != userId)
        {
            return Forbid();
        }

        if (project.Status != ProjectStatus.Paid)
        {
            return BadRequest("Проект не может быть завершён.");
        }

        await _balanceService.ReleaseForProjectAsync(
            project.ClientId,
            project.SelectedFreelancerId,
            acceptBid.Amount,
            project.Id
        );

        project.Status = ProjectStatus.Completed;
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = "Проект завершён.";
        return RedirectToAction(nameof(Details), new { id = project.Id });
    }

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

        if (project.Status != ProjectStatus.Open)
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
}