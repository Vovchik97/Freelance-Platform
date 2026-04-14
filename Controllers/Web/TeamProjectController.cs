using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Dto.Projects;
using FreelancePlatform.Dto.TeamProject;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

[Authorize]
public class TeamProjectController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ProjectActivityLogService _logService;
    private readonly BalanceService _balanceService;

    public TeamProjectController(AppDbContext context, UserManager<IdentityUser> userManager,
        ProjectActivityLogService logService, BalanceService balanceService)
    {
        _context = context;
        _userManager = userManager;
        _logService = logService;
        _balanceService = balanceService;
    }

    public async Task<IActionResult> Details(int id, string tab = "overview")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var project = await _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Include(p => p.Bids)
                .ThenInclude(b => b.Freelancer)
            .Include(p => p.ActivityLogs.OrderByDescending(l => l.CreatedAt))
            .Include(p => p.GroupChatMessages)
                .ThenInclude(m => m.Attachments)
            .Include(p => p.GroupChatMessages)
                .ThenInclude(m => m.Mentions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }

        project.GroupChatMessages = project.GroupChatMessages
            .Where(m => m.ParentMessageId == null)
            .OrderBy(m => m.SentAt)
            .ToList();

        var isMember = project.Members
            .Any(m => m.UserId == userId && m.Status == ProjectMemberStatus.Accepted);
        var isClient = project.ClientId == userId;
        var isLead = project.Members
            .Any(m => m.UserId == userId && m.Role == ProjectMemberRole.Lead &&
                      m.Status == ProjectMemberStatus.Accepted);
        var isFreelancer = User.IsInRole("Freelancer");

        if (!isMember && !isClient)
        {
            if (!isFreelancer)
            {
                return Forbid();
            }

            if (tab != "overview")
            {
                return RedirectToAction(nameof(Details), new { id, tab = "overview" });
            }
        }
        
        var userBid = project.Bids.FirstOrDefault(b => b.FreelancerId == userId);

        var memberUserIds = project.Members.Select(m => m.UserId).ToList();
        var memberUsers = await _userManager.Users
            .Where(u => memberUserIds.Contains(u.Id))
            .ToListAsync();
        
        var acceptedMembers = project.Members
            .Where(m => m.Status == ProjectMemberStatus.Accepted)
            .ToList();

        var unreadCount = await _context.GroupChatMessages
            .Where(m => m.ProjectId == id && m.SenderId != userId && m.ParentMessageId == null &&
                        !m.ReadBy.Any(r => r.UserId == userId))
            .CountAsync();

        ViewBag.Tab = tab;
        ViewBag.IsClient = isClient;
        ViewBag.IsLead = isLead;
        ViewBag.IsMember = isMember;
        ViewBag.IsFreelancerOnly = isFreelancer && !isClient && !isLead && !isMember;
        ViewBag.CurrentUserId = userId;
        ViewBag.CurrentUserName = User.Identity!.Name;
        ViewBag.MemberUsers = memberUsers;
        ViewBag.UserBid = userBid;
        ViewBag.CanManageBids = isClient || isLead;
        ViewBag.AcceptedMembers = acceptedMembers;
        ViewBag.UnreadChatCount = unreadCount;

        return View(project);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InviteMember(InviteMemberDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId);

        if (project == null)
        {
            return NotFound();
        }

        var isClient = project.ClientId == userId;
        var isLead = project.Members.Any(m =>
            m.UserId == userId && m.Role == ProjectMemberRole.Lead && m.Status == ProjectMemberStatus.Accepted);

        if (!isClient && !isLead)
        {
            return Forbid();
        }

        var targetUser = await _userManager.FindByNameAsync(dto.UserName);

        if (targetUser == null)
        {
            TempData["Error"] = "Пользователь не найден.";
            return RedirectToAction(nameof(Details), new { id = dto.ProjectId, tab = "team" });
        }

        var alreadyMember = project.Members.Any(m => m.UserId == targetUser.Id);

        if (alreadyMember)
        {
            TempData["Error"] = "Пользователь уже является участником или приглашён.";
            return RedirectToAction(nameof(Details), new { id = dto.ProjectId, tab = "team" });
        }

        var member = new ProjectMember
        {
            ProjectId = project.Id,
            UserId = targetUser.Id,
            UserName = targetUser.UserName!,
            Role = dto.Role,
            Status = ProjectMemberStatus.Pending,
            JoinedAt = DateTime.UtcNow
        };

        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync();

        await _logService.LogAsync(
            dto.ProjectId,
            $"Пользователь @{targetUser.UserName} приглашён в проект",
            userId,
            User.Identity!.Name
        );

        TempData["Success"] = $"Приглашение отправлено пользователю {targetUser.UserName}.";
        return RedirectToAction(nameof(Details), new { id = dto.ProjectId, tab = "team" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvite(int memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var member = await _context.ProjectMembers
            .Include(m => m.Project)
            .FirstOrDefaultAsync(m => m.Id == memberId && m.UserId == userId);

        if (member == null)
        {
            return NotFound();
        }

        member.Status = ProjectMemberStatus.Accepted;
        member.JoinedAt = DateTime.UtcNow;

        if (member.Project.Status == ProjectStatus.Open)
        {
            member.Project.Status = ProjectStatus.InProgress;
            await _logService.LogAsync(member.ProjectId,
                "Проект переведён в статус «В работе»", userId, member.UserName);
        }
        
        await _context.SaveChangesAsync();

        await _logService.LogAsync(
            member.ProjectId,
            $"@{member.UserName} принял приглашение и вступил в команду",
            userId,
            member.UserName
        );

        TempData["Success"] = "Вы вступили в проект!";
        return RedirectToAction("MyBids", "Bid", new { tab = "invites" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineInvite(int memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var member = await _context.ProjectMembers
            .FirstOrDefaultAsync(m => m.Id == memberId && m.UserId == userId);

        if (member == null)
        {
            return NotFound();
        }

        member.Status = ProjectMemberStatus.Rejected;
        await _context.SaveChangesAsync();

        TempData["Info"] = "Приглашение отклонено";
        return RedirectToAction("MyBids", "Bid", new { tab = "invites" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var member = await _context.ProjectMembers
            .Include(m => m.Project)
            .FirstOrDefaultAsync(m => m.Id == memberId);

        if (member == null)
        {
            return NotFound();
        }

        var project = member.Project;
        var isClient = project.ClientId == userId;
        var isLead = await _context.ProjectMembers
            .AnyAsync(m =>
                m.ProjectId == project.Id && m.UserId == userId && m.Role == ProjectMemberRole.Lead &&
                m.Status == ProjectMemberStatus.Accepted);

        if (!isClient && !isLead)
        {
            return Forbid();
        }

        var removedName = member.UserName;
        _context.ProjectMembers.Remove(member);
        await _context.SaveChangesAsync();

        await _logService.LogAsync(
            project.Id,
            $"@{removedName} удалён из команды",
            userId,
            User.Identity!.Name
        );

        TempData["Success"] = $"Участник {removedName} удалён.";
        return RedirectToAction(nameof(Details), new { id = project.Id, tab = "team" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLead(int memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var member = await _context.ProjectMembers
            .Include(m => m.Project)
                .ThenInclude(p => p.Members)
            .FirstOrDefaultAsync(m => m.Id == memberId);

        if (member == null)
        {
            return NotFound();
        }

        if (member.Project.ClientId != userId)
        {
            return Forbid();
        }

        var currentLead = member.Project.Members
            .FirstOrDefault(m =>
                m.Role == ProjectMemberRole.Lead && m.Id != memberId && m.Status == ProjectMemberStatus.Accepted);

        if (currentLead != null)
        {
            currentLead.Role = ProjectMemberRole.Member;
        }

        member.Role = ProjectMemberRole.Lead;
        await _context.SaveChangesAsync();

        await _logService.LogAsync(
            member.ProjectId,
            currentLead != null 
                ? $"@{member.UserName} назначен новым лидом, @{currentLead.UserName} стал участником"
                : $"@{member.UserName} назначен лидом проекта",
            userId,
            User.Identity!.Name
        );

        TempData["Success"] = $"{member.UserName} назначен Lead.";
        return RedirectToAction(nameof(Details), new { id = member.ProjectId, tab = "team" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTask(CreateTaskDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var project = await _context.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId);

        if (project == null)
        {
            return NotFound();
        }

        if (project.Status != ProjectStatus.InProgress)
        {
            TempData["Error"] = "Создавать задачи можно только когда проект в статусе «В работе».";
            return RedirectToAction(nameof(Details), new { id = dto.ProjectId, tab = "tasks" });
        }

        var isClient = project.ClientId == userId;
        var isLead = project.Members
            .Any(m => m.UserId == userId && m.Role == ProjectMemberRole.Lead &&
                      m.Status == ProjectMemberStatus.Accepted);

        if (!isClient && !isLead)
        {
            return Forbid();
        }

        string? assignedName = null;
        if (!string.IsNullOrEmpty(dto.AssignedToUserId))
        {
            var assignedUser = await _userManager.FindByIdAsync(dto.AssignedToUserId);
            assignedName = assignedUser?.UserName;
        }

        var task = new ProjectTask
        {
            ProjectId = dto.ProjectId,
            Title = dto.Title,
            Description = dto.Description,
            AssignedToUserId = dto.AssignedToUserId,
            AssignedToUserName = assignedName,
            Status = ProjectTaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };
        
        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync();

        await _logService.LogAsync(
            dto.ProjectId,
            $"Создана задача «{task.Title}»" +
            (assignedName != null ? $", назначена @{assignedName}" : ""),
            userId,
            User.Identity!.Name
        );

        TempData["Success"] = "Задача создана.";
        return RedirectToAction(nameof(Details), new { id = dto.ProjectId, tab = "tasks" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTaskStatus(int taskId, int status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var task = await _context.ProjectTasks
            .Include(t => t.Project)
                .ThenInclude(p => p.Members)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            return NotFound();
        }
        
        var isCLient = task.Project.ClientId == userId;
        var isLead = task.Project.Members.Any(m =>
            m.UserId == userId && m.Role == ProjectMemberRole.Lead && m.Status == ProjectMemberStatus.Accepted);
        
        if (isCLient && !isLead)
        {
            return Forbid();
        }
        
        var newStatus = (ProjectTaskStatus) status;
        var isAssigned = task.AssignedToUserId == userId;
        var isMember = task.Project.Members
            .Any(m => m.UserId == userId && m.Status == ProjectMemberStatus.Accepted);

        bool canChangeStatus;
        if (!string.IsNullOrEmpty(task.AssignedToUserId))
        {
            canChangeStatus = isAssigned || isLead;
        }
        else
        {
            canChangeStatus = isLead || isMember;
        }

        if (!canChangeStatus)
        {
            return Forbid();
        }

        if (!isLead)
        {
            var validTransition = (task.Status, newStatus) switch
            {
                (ProjectTaskStatus.Todo, ProjectTaskStatus.InProgress) => true,
                (ProjectTaskStatus.InProgress, ProjectTaskStatus.Done) => true,
                _ => false
            };

            if (!validTransition)
            {
                TempData["Error"] = "Нельзя переместить задачу в этот статус.";
                return RedirectToAction(nameof(Details), new { id = task.ProjectId, tab = "tasks" });
            }
        }

        if (string.IsNullOrEmpty(task.AssignedToUserId) 
            && string.IsNullOrEmpty(task.TakenByUserId) 
            && newStatus == ProjectTaskStatus.InProgress)
        {
            var member = task.Project.Members
                .FirstOrDefault(m => m.UserId == userId);
            task.TakenByUserId = userId;
            task.TakenByUserName = member?.UserName;
        }
        
        task.Status = newStatus;
        await _context.SaveChangesAsync();
        
        await _logService.LogAsync(
            task.ProjectId,
            $"Задача «{task.Title}» переведена в статус «{newStatus switch {
                ProjectTaskStatus.Todo => "К выполнению",
                ProjectTaskStatus.InProgress => "В процессе",
                ProjectTaskStatus.Done => "Выполнено",
                _ => newStatus.ToString()
            }}»",
            userId,
            User.Identity!.Name
        );

        TempData["Success"] = "Статус задачи обновлён.";
        return RedirectToAction(nameof(Details), new { id = task.ProjectId, tab = "tasks" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTask(int taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var task = await _context.ProjectTasks
            .Include(t => t.Project)
                .ThenInclude(p => p.Members)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
        {
            return NotFound();
        }
        
        var isClient = task.Project.ClientId == userId;
        var isLead = task.Project.Members
            .Any(m => m.UserId == userId && m.Role == ProjectMemberRole.Lead && m.Status == ProjectMemberStatus.Accepted);

        if (!isClient && !isLead) return Forbid();

        var projectId = task.ProjectId;
        var title = task.Title;
        
        _context.ProjectTasks.Remove(task);
        await _context.SaveChangesAsync();
        
        await _logService.LogAsync(
            projectId,
            $"Задача «{title}» удалена",
            userId,
            User.Identity!.Name
        );

        TempData["Success"] = "Задача удалена.";
        return RedirectToAction(nameof(Details), new { id = projectId, tab = "tasks" });
    }

    [HttpGet]
    public async Task<IActionResult> GetInviteCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null) return Unauthorized();

        var count = await _context.ProjectMembers
            .CountAsync(m => m.UserId == userId && m.Status == ProjectMemberStatus.Pending);
        
        var json = System.Text.Json.JsonSerializer.Serialize(new { count });

        return Content(json, "application/json");
    }

    [HttpGet]
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
        
        if (project.Status != ProjectStatus.Open)
        {
            return BadRequest("Проект нельзя отменить на текущей стадии."); 
        }

        var dto = new UpdateProjectDto
        {
            Title = project.Title,
            Description = project.Description,
            Budget = project.Budget,
            Status = project.Status,
            CategoryIds = project.Categories.Select(c => c.Id).ToList()
        };

        ViewBag.ProjectId = id;
        ViewBag.AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
        ViewBag.SelectedCategoryIds = dto.CategoryIds; 
        
        return View(dto);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateProjectDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound(); 
        }
        
        if (project.Status != ProjectStatus.Open)
        {
            return BadRequest("Проект нельзя отменить на текущей стадии."); 
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ProjectId = id;
            ViewBag.AllCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.SelectedCategoryIds = dto.CategoryIds;
            return View(dto);
        }

        project.Title = dto.Title;
        project.Description = dto.Description;
        project.Budget = dto.Budget;
        project.Status = dto.Status;
        
        project.Categories.Clear();
        if (dto.CategoryIds != null && dto.CategoryIds.Any())
        {
            var selectedCategories = await _context.Categories
                .Where(c => dto.CategoryIds.Contains(c.Id) && c.IsActive)
                .ToListAsync();
            foreach (var cat in selectedCategories)
            {
                project.Categories.Add(cat);
            }
        }

        await _context.SaveChangesAsync();

        await _logService.LogAsync(id, "Проект отредактирован", userId, User.Identity!.Name);
        
        TempData["Success"] = "Проект отредактирован.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }

        if (project.Status != ProjectStatus.Open)
        {
            TempData["Error"] = "Оплатить можно только открытый проект.";
            return RedirectToAction(nameof(Details), new { id });
        }

        try
        {
            await _balanceService.FreezeForProjectAsync(userId, project.Budget, project.Id);

            project.Status = ProjectStatus.Paid;

            _context.Payments.Add(new Payment
            {
                ProjectId = project.Id,
                PayerId = userId,
                AmountMinor = (long)(project.Budget * 100),
                Currency = "RUB",
                Provider = "Internal",
                Status = PaymentStatus.Succeeded,
                Type = PaymentType.Project
            });
            
            await _context.SaveChangesAsync();
            
            await _logService.LogAsync(id,
                $"Проект оплачен, заморожено " +
                $"{project.Budget.ToString("C", new System.Globalization.CultureInfo("ru-RU"))}",
                userId, User.Identity!.Name);

            TempData["Success"] = "Проект оплачен, средства заморожены. Можно начинать работу.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }

        if (project.Status != ProjectStatus.Paid)
        {
            TempData["Error"] = "Начать работу можно только после оплаты.";
            return RedirectToAction(nameof(Details), new { id });
        }

        project.Status = ProjectStatus.InProgress;
        await _context.SaveChangesAsync();

        await _logService.LogAsync(id, "Работа над проектом начата", userId, User.Identity!.Name);
        
        TempData["Success"] = "Работа над проектом начата.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects
            .Include(p => p.Bids)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }

        if (project.Status != ProjectStatus.Open && project.Status != ProjectStatus.Paid)
        {
            TempData["Error"] = "нельзя отменить проект, который уже в работе.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (project.Status == ProjectStatus.Paid)
        {
            try
            {
                await _balanceService.RefundForProjectAsync(userId, project.Budget, project.Id);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        project.Status = ProjectStatus.Cancelled;
        foreach (var bid in project.Bids.Where(b => b.Status == BidStatus.Pending))
        {
            bid.Status = BidStatus.Rejected;
        }

        await _context.SaveChangesAsync();
        
        await _logService.LogAsync(id,
            project.Status == ProjectStatus.Paid
                ? "Проект отменён, бюджет возвращён клиенту"
                : "Проект отменён",
            userId, User.Identity!.Name);

        TempData["Success"] = project.Status == ProjectStatus.Paid
            ? "Проект отменён, средства возвращены на ваш баланс."
            : "Проект отменён.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResumeProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }

        if (project.Status != ProjectStatus.Cancelled)
        {
            return BadRequest("Проект не в статусе 'Отменён'."); 
        }
        
        project.Status = ProjectStatus.Open;
        await _context.SaveChangesAsync();
        
        await _logService.LogAsync(id, "Проект возобновлен", userId, User.Identity!.Name);

        TempData["Success"] = "Проект возобновлен.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Проект удалён.";
        return RedirectToAction("MyProjects", "Project", new { tab = "team" });
    }

    [HttpGet]
    public async Task<IActionResult> CompleteProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var project = await _context.Projects
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound(); 
        }

        if (project.Status != ProjectStatus.InProgress)
        {
            TempData["Error"] = "Завершить можно только проект в статусе «В работе».";
            return RedirectToAction(nameof(Details), new { id });
        }

        var shares = CalculateShares(project);

        ViewBag.ProjectId = id;
        ViewBag.Budget = project.Budget;
        return View(shares);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCompletion(int projectId, List<string> userIds, List<string> finalSharesRaw)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var project = await _context.Projects
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }

        if (project.Status != ProjectStatus.InProgress)
        {
            TempData["Error"] = "Завершить можно только проект в статусе «В работе».";
            return RedirectToAction(nameof(Details), new { id = projectId });
        }

        var finalShares = new List<decimal>();
        foreach (var raw in finalSharesRaw)
        {
            var cleaned = raw
                .Trim()
                .Replace(" ", "")
                .Replace(",", ".");

            if (!decimal.TryParse(
                    cleaned,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsed))
            {
                TempData["Error"] = $"Некорректное значение суммы: {raw}";
                return RedirectToAction(nameof(CompleteProject), new { id = projectId });
            }
            
            finalShares.Add(Math.Round(parsed, 2));
        }

        var totalFinal = finalShares.Sum();
        if (Math.Abs(totalFinal - project.Budget) > 1)
        {
            TempData["Error"] = "Сумма долей должна равняться бюджету проекта.";
            return RedirectToAction(nameof(CompleteProject), new { id = projectId });
        }

        var autoShares = CalculateShares(project);
        for (int i = 0; i < userIds.Count; i++)
        {
            var auto = autoShares.FirstOrDefault(s => s.UserId == userIds[i]);
            if (auto == null)
            {
                continue;
            }

            var min = auto.AutoShare * 0.8m;
            var max = auto.AutoShare * 1.2m;

            if (finalShares[i] < min || finalShares[i] > max)
            {
                TempData["Error"] =
                    $"Доля участника {auto.UserName} выходит за пределы ±20% " +
                    $"от автоматической ({auto.AutoShare.ToString("C", new System.Globalization.CultureInfo("ru-RU"))}).";
                return RedirectToAction(nameof(CompleteProject), new { id = projectId });
            }
        }

        var payouts = userIds
            .Select((uid, i) =>
            (
                UserId: uid,
                UserName: autoShares.FirstOrDefault(s => s.UserId == uid)?.UserName ?? "",
                Amount: finalShares[i]
            ))
            .ToList();

        try
        {
            await _balanceService.ReleaseForTeamProjectAsync(userId, payouts, projectId);
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(CompleteProject), new { id = projectId });
        }
        
        for (int i = 0; i < userIds.Count; i++)
        {
            var autoShare = autoShares.FirstOrDefault(s => s.UserId == userIds[i]);
                
            _context.PaymentShares.Add(new PaymentShare
            {
                ProjectId = projectId,
                UserId = userIds[i],
                UserName = autoShare?.UserName ?? "",
                IsLead = autoShare?.IsLead ?? false,
                TasksDone = autoShare?.TasksDone ?? 0,
                AutoShare = autoShare?.AutoShare ?? 0,
                FinalShare = finalShares[i],
                CreatedAt = DateTime.UtcNow
            });

            await _logService.LogAsync(projectId,
                $"@{autoShare?.UserName} получил выплату " +
                $"{finalShares[i].ToString("C", new System.Globalization.CultureInfo("ru-RU"))}",
                userId, User.Identity!.Name);
        }
        
        project.Status = ProjectStatus.Completed;
        await _context.SaveChangesAsync();
        
        await _logService.LogAsync(projectId, "Проект завершён, выплаты распределены",
            userId, User.Identity!.Name);

        TempData["Success"] = "Проект завершён, выплаты произведены!";
        return RedirectToAction(nameof(Details), new { id = projectId });
    }
    
    private List<PaymentShare> CalculateShares(Project project)
    {
        var acceptedMembers = project.Members
            .Where(m => m.Status == ProjectMemberStatus.Accepted)
            .ToList();

        if (!acceptedMembers.Any())
        {
            return new List<PaymentShare>();
        }

        var lead = acceptedMembers.FirstOrDefault(m => m.Role == ProjectMemberRole.Lead);
        var budget = project.Budget;

        var tasksByUser = project.Tasks
            .Where(t => t.Status == ProjectTaskStatus.Done)
            .GroupBy(t => t.AssignedToUserId ?? t.TakenByUserId)
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key!, g => g.Count());

        var totalDoneTasks = tasksByUser.Values.Sum();
        
        decimal leadBonus = lead != null ? budget * 0.20m : 0;
        var remainingBudget = budget - leadBonus;

        var shares = new List<PaymentShare>();

        foreach (var member in acceptedMembers)
        {
            var tasksDone = tasksByUser.GetValueOrDefault(member.UserId, 0);

            decimal baseShare;
            if (totalDoneTasks == 0)
            {
                baseShare = remainingBudget / acceptedMembers.Count;
            }
            else
            {
                baseShare = (decimal)tasksDone / totalDoneTasks * remainingBudget;
            }
            
            var isLead = lead?.UserId == member.UserId;
            var autoShare = isLead ? baseShare + leadBonus : baseShare;
            
            shares.Add(new PaymentShare
            {
                ProjectId = project.Id,
                UserId = member.UserId,
                UserName = member.UserName,
                IsLead = isLead,
                TasksDone = tasksDone,
                AutoShare = Math.Round(autoShare, 2),
                FinalShare = Math.Round(autoShare, 2)
            });
        }

        return shares;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> SubmitBid(int projectId, decimal amount, int durationInDays, string? comment)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var project = await _context.Projects
            .Include(p => p.Bids)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            return NotFound();
        }

        if (project.Status != ProjectStatus.Open)
        {
            TempData["Success"] = "Проект недоступен для откликов.";
            return RedirectToAction(nameof(Details), new { id = projectId, tab = "overview" });
        }

        var alreadyBid = project.Bids.Any(b => b.FreelancerId == userId);
        if (alreadyBid)
        {
            TempData["Error"] = "Вы уже откликнулись на этот проект.";
            return RedirectToAction(nameof(Details), new { id = projectId, tab = "overview" });
        }

        var bid = new Bid
        {
            ProjectId = projectId,
            FreelancerId = userId!,
            Amount = amount,
            DurationInDays = durationInDays,
            Comment = comment,
            Status = BidStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();

        await _logService.LogAsync(projectId, $"Фрилансер @{User.Identity!.Name} откликнулся на проект", userId,
            User.Identity!.Name);

        TempData["Success"] = "Заявка отправлена";
        return RedirectToAction(nameof(Details), new { id = projectId, tab = "overview" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptBid(int projectId, int bidId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var project = await _context.Projects
            .Include(p => p.Bids)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            return NotFound();
        }
        
        var isClient = project.ClientId == userId;
        var isLead = project.Members.Any(m =>
            m.UserId == userId && m.Role == ProjectMemberRole.Lead && m.Status == ProjectMemberStatus.Accepted);

        if (!isClient && !isLead)
        {
            return Forbid();
        }
        
        var bid = project.Bids.FirstOrDefault(b => b.Id == bidId);
        if (bid == null)
        {
            return NotFound();
        }
        
        bid.Status = BidStatus.Accepted;

        var alreadyMember = project.Members.Any(m => m.UserId == bid.FreelancerId);
        if (!alreadyMember)
        {
            var freelancer = await _userManager.FindByIdAsync(bid.FreelancerId);
            _context.ProjectMembers.Add(new ProjectMember
            {
                ProjectId = project.Id,
                UserId = bid.FreelancerId,
                UserName = freelancer?.UserName ?? "",
                Role = ProjectMemberRole.Member,
                Status = ProjectMemberStatus.Accepted,
                JoinedAt = DateTime.UtcNow
            });
        }

        if (project.Status == ProjectStatus.Open)
        {
            project.Status = ProjectStatus.InProgress;
            await _logService.LogAsync(projectId,
                "Проект переведён в статус «В работе»", userId, User.Identity!.Name);
        }

        await _context.SaveChangesAsync();

        await _logService.LogAsync(projectId,
            $"Заявка от @{(await _userManager.FindByIdAsync(bid.FreelancerId))?.UserName} принята", userId,
            User.Identity!.Name);
        
        TempData["Success"] = "Заявка принята, участник добавлен в команду.";
        return RedirectToAction(nameof(Details), new { id = projectId, tab = "overview" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectBid(int projectId, int bidId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var project = await _context.Projects
            .Include(p => p.Bids)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
        {
            return NotFound();
        }
        
        var isClient = project.ClientId == userId;
        var isLead = project.Members.Any(m =>
            m.UserId == userId && m.Role == ProjectMemberRole.Lead && m.Status == ProjectMemberStatus.Accepted);

        if (!isClient && !isLead)
        {
            return Forbid();
        }
        
        var bid = project.Bids.FirstOrDefault(b => b.Id == bidId);
        if (bid == null)
        {
            return NotFound();
        }
        
        bid.Status = BidStatus.Rejected;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Заявка отклонена.";
        return RedirectToAction(nameof(Details), new { id = projectId, tab = "overview" });
    }

    [HttpGet]
    public async Task<IActionResult> GetGroupUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var memberProjectIds = await _context.ProjectMembers
            .Where(m => m.UserId == userId && m.Status == ProjectMemberStatus.Accepted)
            .Select(m => m.ProjectId)
            .ToListAsync();

        var clientProjectIds = await _context.Projects
            .Where(p => p.ClientId == userId && p.IsTeamProject)
            .Select(p => p.Id)
            .ToListAsync();

        var allProjectIds = memberProjectIds
            .Concat(clientProjectIds)
            .Distinct()
            .ToList();

        var unreadByProject = new Dictionary<int, int>();

        foreach (var projectId in allProjectIds)
        {
            var count = await _context.GroupChatMessages
                .Where(m => m.ProjectId == projectId && m.SenderId != userId && m.ParentMessageId == null &&
                            !m.ReadBy.Any(r => r.UserId == userId))
                .CountAsync();

            if (count > 0)
            {
                unreadByProject[projectId] = count;
            }
        }

        var total = unreadByProject.Values.Sum();

        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            total,
            byProject = unreadByProject
        });

        return Content(json, "application/json");
    }
}