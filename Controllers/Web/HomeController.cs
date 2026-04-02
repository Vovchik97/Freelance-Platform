using System.Diagnostics;
using System.Security.Claims;
using FreelancePlatform.Context;
using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;
    private readonly RecommendationService _recommendationService;

    public HomeController(ILogger<HomeController> logger, AppDbContext context, RecommendationService recommendationService)
    {
        _logger = logger;
        _context = context;
        _recommendationService = recommendationService;
    }

    public async Task<IActionResult> Index()
    {
        var projects = await _context.Projects
            .Include(p => p.Client)
            .ToListAsync();

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (User.IsInRole("Freelancer"))
            {
                ViewBag.Recommendations = await _recommendationService
                    .GetRecommendedProjectsForFreelancerAsync(userId!);
            }
            else if (User.IsInRole("Client"))
            {
                ViewBag.Recommendations = await _recommendationService
                    .GetRecommendedServicesForClientAsync(userId!);
            }
        }
        
        return View(projects);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}