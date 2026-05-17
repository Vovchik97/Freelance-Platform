using System.Globalization;
using FreelancePlatform.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Data;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Identity;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Analytics;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AnalyticsService _analyticsService;

    public IndexModel(
        AppDbContext context, 
        RoleManager<IdentityRole> roleManager, 
        UserManager<IdentityUser> userManager,
        AnalyticsService analyticsService)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _analyticsService = analyticsService;
    }

    public int UserCount { get; set; }
    public int FreelancerCount { get; set; }
    public int ClientCount { get; set; }
    public int ProjectCount { get; set; }
    public int ServiceCount { get; set; }
    public int ApplicationCount { get; set; }
    public int OrderCount { get; set; }
    
    public string[] Months { get; set; } = new string[6];
    public decimal[] RevenueByMonth { get; set; } = new decimal[6];
    public int[] UsersByMonth { get; set; } = new int[6];
    
    public decimal[] RevenueForecast { get; set; } = new decimal[3];
    public string[] ForecastMonths { get; set; } = new string[3];
    public List<AnomalyPoint> RevenueAnomalies { get; set; } = new();
    public GaussianGrowthAnalysis UserGrowthAnalysis { get; set; } = new();
    public RetentionData Retention { get; set; } = new();
    public PlatformHealthIndex HealthIndex { get; set; } = new();

    public string[] MonthNames { get; set; } = new string[5];
    public string[] ForecastMonthNames { get; set; } = new string[3];

    public async Task OnGetAsync()
    {
        var now = DateTime.UtcNow;
        
        ClientCount = (await _userManager.GetUsersInRoleAsync("Client")).Count;
        FreelancerCount = (await _userManager.GetUsersInRoleAsync("Freelancer")).Count;
        UserCount = await _context.Users.CountAsync();
        ProjectCount = await _context.Projects.CountAsync();
        ServiceCount = await _context.Services.CountAsync();
        ApplicationCount = await _context.Bids.CountAsync();
        OrderCount = await _context.Orders.CountAsync();

        for (int i = 0; i < 6; i++)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-(5 - i));
            Months[i] = month.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));

            var revenueOrders = await _context.Orders
                .Where(o => o.CreatedAt.Year == month.Year
                         && o.CreatedAt.Month == month.Month
                         && o.Status == OrderStatus.Completed)
                .SumAsync(o => (decimal?)o.Service!.Price) ?? 0;

            var revenueBids = await _context.Bids
                .Where(b => b.CreatedAt.Year == month.Year
                         && b.CreatedAt.Month == month.Month
                         && b.Status == BidStatus.Accepted)
                .SumAsync(b => (decimal?)b.Amount) ?? 0;

            RevenueByMonth[i] = revenueOrders + revenueBids;
            
            UsersByMonth[i] = await _context.UserMetadata
                .CountAsync(um => um.RegisteredAt.Year == month.Year
                                && um.RegisteredAt.Month == month.Month);
        }

        for (int i = 1; i < 6; i++)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-(5 - i));
            MonthNames[i - 1] = month.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));
        }

        for (int i = 0; i < 3; i++)
        {
            var forecastMonth = new DateTime(now.Year, now.Month, 1).AddMonths(i + 1);
            ForecastMonthNames[i] = forecastMonth.ToString("MMMM", CultureInfo.GetCultureInfo("ru-RU"));
        }

        // ====== 1. ПРОГНОЗ ОБОРОТА (ETS) ======
        RevenueForecast = _analyticsService.ExponentialSmoothing(RevenueByMonth, 3, alpha: 0.3);
        
        ForecastMonths = Enumerable.Range(1, 3)
            .Select(i => new DateTime(now.Year, now.Month, 1)
                .AddMonths(i)
                .ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU")))
            .ToArray();

        // ====== 2. АНОМАЛИИ В ОБОРОТЕ ======
        RevenueAnomalies = _analyticsService.DetectAnomalies(RevenueByMonth, Months, threshold: 2.0);

        // ====== 3. ГАУССОВ АНАЛИЗ РОСТА ПОЛЬЗОВАТЕЛЕЙ ======
        UserGrowthAnalysis = _analyticsService.AnalyzeUserGrowthWithGaussian(
            UsersByMonth,
            targetUsers: 500
        );

        // ====== 4. RETENTION ======
        var registrationsByMonth = new Dictionary<int, int>();
        var activeByMonth = new Dictionary<int, int>();

        for (int i = 0; i < 6; i++)
        {
            var month = new DateTime(now.Year, now.Month, 1).AddMonths(-(5 - i));
            
            registrationsByMonth[i] = await _context.UserMetadata
                .CountAsync(um => um.RegisteredAt.Year == month.Year 
                                  && um.RegisteredAt.Month == month.Month);
            
            var activeBids = await _context.Bids
                .Where(b => b.CreatedAt.Year == month.Year && b.CreatedAt.Month == month.Month)
                .Select(b => b.FreelancerId)
                .Distinct()
                .CountAsync();

            var activeOrders = await _context.Orders
                .Where(o => o.CreatedAt.Year == month.Year && o.CreatedAt.Month == month.Month)
                .Select(o => o.ClientId)
                .Distinct()
                .CountAsync();

            activeByMonth[i] = activeBids + activeOrders;
        }

        Retention = _analyticsService.CalculateRetention(registrationsByMonth, activeByMonth);

        // ====== 5. ИНДЕКС ЗДОРОВЬЯ ======
        var completedProjects = await _context.Projects
            .CountAsync(p => p.Status == ProjectStatus.Completed);

        HealthIndex = _analyticsService.CalculateHealthIndex(new HealthInputData
        {
            TotalProjects = ProjectCount,
            CompletedProjects = completedProjects,
            CurrentMonthRevenue = RevenueByMonth[5],
            PreviousMonthRevenue = RevenueByMonth[4],
            TotalUsers = UserCount,
            TotalBids = ApplicationCount,
            TotalOrders = OrderCount,
            RetentionRate = Retention.AverageRetention
        });
    }
}