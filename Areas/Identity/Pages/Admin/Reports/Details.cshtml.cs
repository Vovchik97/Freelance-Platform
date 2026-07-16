using FreelancePlatform.Context;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Reports;

/// <summary>
/// Модель страницы просмотра и обработки жалобы.
/// Позволяет администраторам просматривать детали жалобы,
/// изменять её статус, применять санкции и обновлять репутацию пользователя.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IReputationService _reputationService;

    public DetailsModel(AppDbContext context,
        UserManager<IdentityUser> userManager,
        IReputationService reputationService)
    {
        _context = context;
        _userManager = userManager;
        _reputationService = reputationService;
    }
    
    public Report? Report { get; set; }
    public IdentityUser? Reporter { get; set; }
    public IdentityUser? Reported { get; set; }
    public bool IsReportedBanned { get; set; }
    
    [BindProperty] public ReportStatus NewStatus { get; set; }
    [BindProperty] public string? AdminComment { get; set; }
    [BindProperty] public bool ApplyReputation { get; set; }
    [BindProperty] public bool BanUser { get; set; }

    /// <summary>
    /// Загружает данные жалобы по идентификатору,
    /// включая информацию о заказе, проекте и пользователях.
    /// </summary>
    /// <param name="id">Идентификатор жалобы.</param>
    /// <returns>Страница с деталями жалобы или 404 если жалоба не найдена.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Report = await _context.Reports
            .Include(r => r.Order)
                .ThenInclude(o => o!.Service)
            .Include(r => r.Project)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (Report == null)
        {
            return NotFound();
        }

        Reporter = await _userManager.FindByIdAsync(Report.ReporterId);
        Reported = await _userManager.FindByIdAsync(Report.ReportedId);

        IsReportedBanned = Reported != null
            && Reported.LockoutEnabled
            && Reported.LockoutEnd > DateTimeOffset.UtcNow;
        
        return Page();
    }

    /// <summary>
    /// Обрабатывает жалобу:
    /// обновляет статус, сохраняет комментарий администратора,
    /// при необходимости изменяет репутацию и блокирует пользователя.
    /// </summary>
    /// <param name="id">Идентификатор жалобы.</param>
    /// <returns>Перенаправление на страницу списка жалоб или 404 если жалоба не найдена.</returns>
    public async Task<IActionResult> OnPostResolveAsync(int id)
    {
        Report = await _context.Reports.FindAsync(id);
        if (Report == null)
        {
            return NotFound();
        }

        Report.Status = NewStatus;
        Report.AdminComment = AdminComment;
        Report.UpdatedAt = DateTime.UtcNow;

        await ApplyReputationAsync(Report);

        await BanReportedUserAsync(Report.ReportedId);
        
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Жалоба обновлена";

        return RedirectToPage("Index");
    }

    /// <summary>
    /// Применяет штраф репутации к пользователю, если администратор выбрал эту опцию.
    /// Сохраняет событие в историю репутации с типом "ReportResolved".
    /// </summary>
    /// <param name="report">Обработанная жалоба с информацией о пользователе и комментарием.</param>
    private async Task ApplyReputationAsync(Report report)
    {
        if (!ApplyReputation)
        {
            return;
        }
        
        await _reputationService.AddEventAsync(
            report.ReportedId,
            ReputationEventType.ReportResolved,
            report.OrderId,
            report.ProjectId,
            AdminComment
        );
    }

    /// <summary>
    /// Блокирует пользователя бесконечно, если администратор выбрал эту опцию.
    /// Устанавливает LockoutEnd на максимально возможное значение.
    /// </summary>
    /// <param name="userId">Идентификатор блокируемого пользователя.</param>
    private async Task BanReportedUserAsync(string userId)
    {
        if (!BanUser)
        {
            return;
        }
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            await _userManager.UpdateAsync(user);
        }
    }
}