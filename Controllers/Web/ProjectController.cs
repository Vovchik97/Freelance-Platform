using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Api;
using FreelancePlatform.Dto.Projects;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

public class ProjectController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ProjectController(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, string? status, decimal? minBudget, decimal? maxBudget, string sort)
    {
        var query = _context.Projects.Include(p => p.Client).AsQueryable();

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
        
        if (sort == "budget_desc")
            query = query.OrderByDescending(p => p.Budget);
        else if (sort == "budget_asc")
            query = query.OrderBy(p => p.Budget);
        else
            query = query.OrderByDescending(p => p.CreatedAt);
        
        var projects = await query.ToListAsync();

        ViewBag.Searcg = search;
        ViewBag.Status = status;
        ViewBag.MinBudget = minBudget;
        ViewBag.MaxBudget = maxBudget;
        ViewBag.Sort = sort;
        
        return View(projects);
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Client)
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
    public IActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProjectDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }
        
        var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (clientId == null)
        {
            return Unauthorized();
        }

        var project = new Project
        {
            Title = dto.Title,
            Description = dto.Description,
            Budget = dto.Budget,
            Status = dto.Status,
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow
        };
        
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(MyProjects));
    }
    
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);
        
        if (project == null)
        {
            return NotFound();
        }

        var dto = new UpdateProjectDto
        {
            Title = project.Title,
            Description = project.Description,
            Budget = project.Budget,
            Status = project.Status
        };
        
        return View(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateProjectDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }
        
        project.Title = dto.Title;
        project.Description = dto.Description;
        project.Budget = dto.Budget;
        project.Status = dto.Status;

        await _context.SaveChangesAsync();
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
            .Where(p => p.ClientId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

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
        
        if (project == null)
        {
            return NotFound();
        }

        if (project.SelectedFreelancerId != userId)
        {
            return Forbid();
        }

        if (project.Status != ProjectStatus.InProgress)
        {
            return BadRequest("Проект не может быть завершён.");
        }

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
}