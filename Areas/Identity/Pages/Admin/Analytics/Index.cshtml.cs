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

/// <summary>
/// Модель административной страницы аналитики.
/// Загружает статистические данные платформы,
/// прогнозы оборота, показатели роста пользователей
/// и оценку состояния системы.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AnalyticsService _analyticsService;

    public IndexModel(
        AppDbContext context,
        UserManager<IdentityUser> userManager,
        AnalyticsService analyticsService)
    {
        _context = context;
        _userManager = userManager;
        _analyticsService = analyticsService;
    }

    public int UserCount { get; set; }
    public int FreelancerCount { get; set; }
    public int ClientCount { get; set; }
    public int ProjectCount { get; set; }
    public int ServiceCount { get; set; }
    public int BidCount { get; set; }
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

    /// <summary>
    /// Выполняет загрузку всех аналитических данных
    /// при открытии страницы администратора.
    /// </summary>
    public async Task OnGetAsync()
    {
        await LoadCountersAsync();
        await LoadRevenueStatisticsAsync();
        await LoadForecastAsync();
        await LoadRetentionAsync();
        await LoadHealthIndexAsync();
    }

    private async Task LoadCountersAsync()
    {
        ClientCount = (await _userManager.GetUsersInRoleAsync("Client")).Count;
        FreelancerCount = (await _userManager.GetUsersInRoleAsync("Freelancer")).Count;
        UserCount = await _context.Users.CountAsync();
        ProjectCount = await _context.Projects.CountAsync();
        ServiceCount = await _context.Services.CountAsync();
        BidCount = await _context.Bids.CountAsync();
        OrderCount = await _context.Orders.CountAsync();
    }

    /// <summary>
    /// Загружает статистику оборота платформы
    /// за последние 6 месяцев и данные регистрации пользователей.
    /// </summary>
    private async Task LoadRevenueStatisticsAsync()
    {
        var now = DateTime.UtcNow;
        
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
    }
    
    /// <summary>
    /// Выполняет аналитические расчёты:
    /// прогнозирование оборота методом ETS,
    /// поиск аномалий и анализ роста пользователей.
    /// </summary>
    private async Task LoadForecastAsync()
    {
        var now = DateTime.UtcNow;
        
        for (int i = 0; i < 3; i++)
        {
            var forecastMonth = new DateTime(now.Year, now.Month, 1).AddMonths(i + 1);
            ForecastMonthNames[i] = forecastMonth.ToString("MMMM", CultureInfo.GetCultureInfo("ru-RU"));
        }
        
        // Расчёт прогноза оборота ETS
        RevenueForecast = _analyticsService.ExponentialSmoothing(RevenueByMonth, 3, alpha: 0.3);
        
        ForecastMonths = Enumerable.Range(1, 3)
            .Select(i => new DateTime(now.Year, now.Month, 1)
                .AddMonths(i)
                .ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU")))
            .ToArray();

        // Поиск аномальных изменений оборота
        RevenueAnomalies = _analyticsService.DetectAnomalies(RevenueByMonth, Months, threshold: 2.0);

        // Анализ роста пользователей
        UserGrowthAnalysis = _analyticsService.AnalyzeUserGrowthWithGaussian(
            UsersByMonth,
            targetUsers: 500
        );
    }

    /// <summary>
    /// Рассчитывает показатель удержания пользователей
    /// на основе регистраций и активности за последние месяцы.
    /// </summary>
    private async Task LoadRetentionAsync()
    {
        var now = DateTime.UtcNow;
        
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
    }

    /// <summary>
    /// Рассчитывает индекс здоровья платформы
    /// на основе активности пользователей,
    /// оборота и количества выполненных проектов.
    /// </summary>
    private async Task LoadHealthIndexAsync()
    {
        var completedProjects = await _context.Projects
            .CountAsync(p => p.Status == ProjectStatus.Completed);

        HealthIndex = _analyticsService.CalculateHealthIndex(new HealthInputData
        {
            TotalProjects = ProjectCount,
            CompletedProjects = completedProjects,
            CurrentMonthRevenue = RevenueByMonth[5],
            PreviousMonthRevenue = RevenueByMonth[4],
            TotalUsers = UserCount,
            TotalBids = BidCount,
            TotalOrders = OrderCount,
            RetentionRate = Retention.AverageRetention
        });
    }
}