using FreelancePlatform.Context;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Reports;

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

        if (ApplyReputation)
        {
            await _reputationService.AddEventAsync(
                Report.ReportedId,
                ReputationEventType.ReportResolved,
                Report.OrderId,
                Report.ProjectId,
                AdminComment
            );
        }

        if (BanUser)
        {
            var userToBan = await _userManager.FindByIdAsync(Report.ReportedId);
            if (userToBan != null)
            {
                userToBan.LockoutEnabled = true;
                userToBan.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                await _userManager.UpdateAsync(userToBan);
            }
        }
        
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Жалоба обновлена";

        return RedirectToPage("Index");
    }
}