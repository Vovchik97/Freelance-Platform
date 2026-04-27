using FreelancePlatform.Context;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

[Authorize(Roles = "Client, Freelancer")]
public class ReportController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IReputationService _reputationService;

    public ReportController(AppDbContext context,
        UserManager<IdentityUser> userManager,
        IReputationService reputationService)
    {
        _context = context;
        _userManager = userManager;
        _reputationService = reputationService;
    }
    
    [HttpGet]
    public async Task<IActionResult> Create(string reportedId, int? orderId, int? projectId)
    {
        var userId = _userManager.GetUserId(User);

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string reportedId, ReportReason reason,
        string? description, int? orderId, int? projectId)
    {
        var userId = _userManager.GetUserId(User);

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