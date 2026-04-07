using System.Security.Claims;
using FreelancePlatform.Context;
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

    public TeamProjectController(AppDbContext context, UserManager<IdentityUser> userManager,
        ProjectActivityLogService logService)
    {
        _context = context;
        _userManager = userManager;
        _logService = logService;
    }

    public async Task<IActionResult> Details(int id, string tab = "overview")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var project = await _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
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

        if (!isMember && !isClient)
        {
            return Forbid();
        }

        var memberUserIds = project.Members.Select(m => m.UserId).ToList();
        var memberUsers = await _userManager.Users
            .Where(u => memberUserIds.Contains(u.Id))
            .ToListAsync();

        ViewBag.Tab = tab;
        ViewBag.IsClient = isClient;
        ViewBag.IsLead = isLead;
        ViewBag.IsMember = isMember;
        ViewBag.CurrentUserId = userId;
        ViewBag.CurrentUserName = User.Identity!.Name;
        ViewBag.MemberUsers = memberUsers;

        var acceptedMembers = project.Members
            .Where(m => m.Status == ProjectMemberStatus.Accepted)
            .ToList();
        ViewBag.AcceptedMembers = acceptedMembers;

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
        await _context.SaveChangesAsync();

        await _logService.LogAsync(
            member.ProjectId,
            $"@{member.UserName} принял приглашение и вступил в команду",
            userId,
            member.UserName
        );

        TempData["Success"] = "Вы вступили в проект!";
        return RedirectToAction(nameof(Details), new { id = member.ProjectId, tab = "team" });
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
        return RedirectToAction("Index", "Project");
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
    public async Task<IActionResult> SetLEad(int memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var member = await _context.ProjectMembers
            .Include(m => m.Project)
            .FirstOrDefaultAsync(m => m.Id == memberId);

        if (member == null)
        {
            return NotFound();
        }

        if (member.Project.ClientId != userId)
        {
            return Forbid();
        }

        member.Role = ProjectMemberRole.Lead;
        await _context.SaveChangesAsync();

        await _logService.LogAsync(
            member.ProjectId,
            $"@{member.UserName} назначен лидом проекта",
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
            CreatedAt = DateTime.UtcNow
        };

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
        
        await _logService.LogAsync(
            projectId,
            $"Задача «{title}» удалена",
            userId,
            User.Identity!.Name
        );

        TempData["Success"] = "Задача удалена.";
        return RedirectToAction(nameof(Details), new { id = projectId, tab = "tasks" });
    }

    public async Task<IActionResult> MyInvites()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var invites = await _context.ProjectMembers
            .Include(m => m.Project)
            .Where(m => m.UserId == userId && m.Status == ProjectMemberStatus.Pending)
            .ToListAsync();

        return View(invites);
    }

    public async Task<IActionResult> MyTeamProjects()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var memberProjectIds = await _context.ProjectMembers
            .Where(m => m.UserId == userId && m.Status == ProjectMemberStatus.Accepted)
            .Select(m => m.ProjectId)
            .ToListAsync();

        var projects = await _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Members)
            .Where(p => memberProjectIds.Contains(p.Id) || p.ClientId == userId)
            .Where(p => p.IsTeamProject)
            .ToListAsync();

        return View(projects);
    }

    [HttpGet]
    public async Task<IActionResult> GetInviteCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        var count = await _context.ProjectMembers
            .CountAsync(m => m.UserId == userId && m.Status == ProjectMemberStatus.Pending);

        return Json(new { count });
    }
}