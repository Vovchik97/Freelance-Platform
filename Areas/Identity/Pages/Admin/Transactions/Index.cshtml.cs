using FreelancePlatform.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Stripe;
using BalanceTransaction = FreelancePlatform.Models.BalanceTransaction;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Transactions;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    
    public IndexModel(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<(BalanceTransaction Transaction, string? UserEmail)> Transactions { get; set; } = new();

    public async Task OnGetAsync()
    {
        var transactions = await _context.BalanceTransactions
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        var userIds = transactions.Select(t => t.UserId).Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);

        Transactions = transactions.Select(t => (t, users.GetValueOrDefault(t.UserId))).ToList();
    }
}