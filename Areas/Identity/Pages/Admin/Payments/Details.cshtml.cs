using FreelancePlatform.Context;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Payments;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IBalanceService _balanceService;
    private readonly UserManager<IdentityUser> _userManager;
    
    public DetailsModel(AppDbContext context, IBalanceService balanceService, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _balanceService = balanceService;
        _userManager = userManager;
    }

    public Payment Payment { get; set; } = null!;
    public string? PayerEmail { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }

        Payment = payment;
        var user = await _userManager.FindByIdAsync(payment.PayerId);
        PayerEmail = user?.Email;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }

        var amount = payment.AmountMinor / 100m;

        if (payment.OrderId.HasValue)
        {
            await _balanceService.RefundForOrderAsync(payment.PayerId, amount, payment.OrderId.Value);
        }
        
        else if (payment.ProjectId.HasValue)
        {
            await _balanceService.RefundForProjectAsync(payment.PayerId, amount, payment.ProjectId.Value);
        }
        
        return RedirectToPage(new { id });
    }
}