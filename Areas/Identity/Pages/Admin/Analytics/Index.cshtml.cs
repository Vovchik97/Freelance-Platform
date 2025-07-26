using System.Globalization;
using FreelancePlatform.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Data;
using FreelancePlatform.Models;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Analytics;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public int UserCount { get; set; }
    public int FreelancerCount { get; set; }
    public int ClientCount { get; set; }
    public int ProjectCount { get; set; }
    public int ServiceCount { get; set; }
    public int ApplicationCount { get; set; }
    public int OrderCount { get; set; }
    
    public string[] Months { get; set; } = new string[6];
    public int[] ProjectsByMonth { get; set; } = new int[6];
    public decimal[] RevenueByMonth { get; set; } = new decimal[6];

    public async Task OnGetAsync()
    {
        UserCount = await _context.Users.CountAsync();
        FreelancerCount = await _context.UserRoles.CountAsync(u => u.RoleId == "da11a244-e04c-44f2-894e-96be625d64cf");
        ClientCount = await _context.UserRoles.CountAsync(u => u.RoleId == "5e82848c-349d-4deb-8510-240d8aa73aa8");
        ProjectCount = await _context.Projects.CountAsync();
        ServiceCount = await _context.Services.CountAsync();
        ApplicationCount = await _context.Bids.CountAsync();
        OrderCount = await _context.Orders.CountAsync();
        
        // последние 6 месяцев
        var now = DateTime.UtcNow;
        for (int i = 0; i < 6; i++)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-(5-i));
            string monthLabel = month.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));
            Months[i] = monthLabel;

            // доход
            var revenueOrders = await _context.Orders
                .Where(o => o.CreatedAt.Year == month.Year && o.CreatedAt.Month == month.Month && o.Status == OrderStatus.Completed)
                .SumAsync(o => (decimal?)o.Service!.Price) ?? 0;
            var revenueBids = await _context.Bids
                .Where(b => b.CreatedAt.Year == month.Year && b.CreatedAt.Month == month.Month && b.Status == BidStatus.Accepted)
                .SumAsync(b => (decimal?)b.Amount) ?? 0;
            var revenue = revenueOrders + revenueBids;
            RevenueByMonth[i] = revenue;
        }
    }
}