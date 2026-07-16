using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin;

/// <summary>
/// Модель главной страницы административной панели.
/// Загружает общие показатели для администратора:
/// новые обращения поддержки и количество ожидающих жалоб.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }
    
    /// <summary>
    /// Загружает основные показатели административной панели:
    /// наличие новых обращений поддержки и количество ожидающих жалоб.
    /// </summary>
    public async Task OnGetAsync()
    {
        var hasNewSupport = await _context.Chats.AnyAsync(c => c.IsSupport && !c.IsBotActive);
        var pendingReportsCount = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending);
        ViewData["HasNewSupport"] = hasNewSupport;
        ViewData["PendingReportsCount"] = pendingReportsCount;
    }
}