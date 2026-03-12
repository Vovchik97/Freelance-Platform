using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Finance;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly AppDbContext _context;
    
    public DashboardModel(AppDbContext context)
    {
        _context = context;
    }
    
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalRefunds { get; set; }
    public decimal TotalFrozen{ get; set; }
    public decimal TotalUserBalances { get; set; }
    public int PaymentsCount { get; set; }
    public int TransactionsCount { get; set; }

    public async Task OnGetAsync()
    {
        TotalDeposits = await _context.BalanceTransactions
            .Where(t => t.Type == BalanceTransactionType.Deposit)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        
        TotalRefunds = await _context.BalanceTransactions
            .Where(t => t.Type == BalanceTransactionType.Refund)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;
        
        TotalWithdrawals = await _context.BalanceTransactions
            .Where(t => t.Type == BalanceTransactionType.Withdraw)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        TotalFrozen = await _context.UserBalances
            .SumAsync(b => (decimal?)b.Frozen) ?? 0;

        TotalUserBalances = await _context.UserBalances
            .SumAsync(b => (decimal?)b.Balance) ?? 0;

        PaymentsCount = await _context.Payments.CountAsync();
        TransactionsCount = await _context.BalanceTransactions.CountAsync();
    }
}