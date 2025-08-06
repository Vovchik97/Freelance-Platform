using System.Globalization;
using FreelancePlatform.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Data;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Identity;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Analytics;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(AppDbContext context, RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
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
        var clientRole = await _roleManager.FindByNameAsync("Client");
        var freelancerRole = await _roleManager.FindByNameAsync("Freelancer");

        if (clientRole == null || freelancerRole == null)
        {
            ClientCount = 0;
            FreelancerCount = 0;
        }
        else
        {
            ClientCount = (await _userManager.GetUsersInRoleAsync("Client")).Count;
            FreelancerCount = (await _userManager.GetUsersInRoleAsync("Freelancer")).Count;
        }
        UserCount = await _context.Users.CountAsync();
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