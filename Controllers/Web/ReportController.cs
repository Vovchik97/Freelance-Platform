using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

/// <summary>
/// Контроллер управления жалобами пользователей.
/// Позволяет создавать жалобы на других пользователей
/// и просматривать историю отправленных жалоб.
/// </summary>
[Authorize(Roles = "Client, Freelancer")]
public class ReportController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ReportController(AppDbContext context,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    /// <summary>
    /// Отображает форму создания жалобы.
    /// Проверяет наличие уже существующей активной жалобы от текущего пользователя на указанного пользователя.
    /// </summary>
    /// <param name="reportedId">Идентификатор пользователя, на которого создаётся жалоба.</param>
    /// <param name="orderId">Идентификатор связанного заказа, если жалоба относится к заказу.</param>
    /// <param name="projectId">Идентификатор связанного проекта, если жалоба относится к проекту.</param>
    /// <returns>Представление формы создания жалобы.</returns>
    [HttpGet]
    public async Task<IActionResult> Create(string reportedId, int? orderId, int? projectId)
    {
        var userId = _userManager.GetUserId(User);
        var reportedUser = await _userManager.FindByIdAsync(reportedId);
        
        if (userId == null)
        {
            return Unauthorized();
        }
        
        if (reportedUser == null)
        {
            return NotFound();
        }

        var hasActiveReport = userId != null && await _context.Reports.AnyAsync(r =>
            r.ReporterId == userId &&
            r.ReportedId == reportedId &&
            r.Status == ReportStatus.Pending);
        
        ViewBag.ReportedId = reportedId;
        ViewBag.OrderId = orderId;
        ViewBag.ProjectId = projectId;
        ViewBag.HasActiveReport = hasActiveReport;
        return View();
    }

    /// <summary>
    /// Создаёт новую жалобу пользователя.
    /// Проверяет корректность отправителя, наличие существующей активной жалобы и сохраняет обращение в базу данных.
    /// </summary>
    /// <param name="reportedId">Идентификатор пользователя, на которого отправляется жалоба.</param>
    /// <param name="reason">Причина создания жалобы.</param>
    /// <param name="description">Дополнительное описание ситуации.</param>
    /// <param name="orderId">Идентификатор связанного заказа, если он существует.</param>
    /// <param name="projectId">Идентификатор связанного проекта, если он существует.</param>
    /// <returns>Перенаправление на главную страницу или форму создания жалобы при наличии ошибки.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string reportedId, ReportReason reason,
        string? description, int? orderId, int? projectId)
    {
        var userId = _userManager.GetUserId(User);
        var reportedUser = await _userManager.FindByIdAsync(reportedId);

        if (reportedUser == null)
        {
            return NotFound();
        }

        if (userId == null)
        {
            return Unauthorized();
        }
        
        if (userId == reportedId)
        {
            TempData["ErrorMessage"] = "Нельзя пожаловаться на самого себя";
            return RedirectToAction("Index", "Home");
        }
        
        var alreadyReported = await _context.Reports.AnyAsync(r =>
            r.ReporterId == userId &&
            r.ReportedId == reportedId && 
            r.Status == ReportStatus.Pending);

        if (alreadyReported)
        {
            TempData["ErrorMessage"] = "Вы уже подали жалобу на этого пользователя";
            return RedirectToAction("Create", new { reportedId, orderId, projectId });
        }
        
        _context.Reports.Add(new Report
        {
            ReporterId = userId,
            ReportedId = reportedId,
            Reason = reason,
            Description = description,
            OrderId = orderId,
            ProjectId = projectId
        });
        
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Жалоба отправлена на рассмотрение";

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Отображает список жалоб, отправленных текущим пользователем.
    /// Загружает информацию о связанных заказах, проектах и пользователях, на которых были отправлены жалобы.
    /// </summary>
    /// <returns>Представление со списком отправленных жалоб пользователя.</returns>
    [HttpGet]
    public async Task<IActionResult> My()
    {
        var userId = _userManager.GetUserId(User);

        var reports = await _context.Reports
            .Include(r => r.Order)
            .Include(r => r.Project)
            .Where(r => r.ReporterId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        var reportedIds = reports.Select(r => r.ReportedId).Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => reportedIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName);

        ViewBag.ReportedUsers = users;

        return View(reports);
    }
}