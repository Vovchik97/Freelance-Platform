using System.Diagnostics;
using System.Security.Claims;
using FreelancePlatform.Context;
using Microsoft.AspNetCore.Mvc;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

/// <summary>
/// Контроллер главной страницы платформы.
/// Отвечает за отображение проектов,
/// персональных рекомендаций и системных страниц.
/// </summary>
public class HomeController : Controller
{
    private readonly AppDbContext _context;
    private readonly RecommendationService _recommendationService;
    
    public HomeController(AppDbContext context, RecommendationService recommendationService)
    {
        _context = context;
        _recommendationService = recommendationService;
    }

    /// <summary>
    /// Отображает главную страницу платформы.
    /// Загружает список доступных проектов и персональные рекомендации
    /// для авторизованных клиентов и исполнителей.
    /// </summary>
    /// <returns>Представление главной страницы с данными проектов.</returns>
    public async Task<IActionResult> Index()
    {
        var projects = await _context.Projects
            .Include(p => p.Client)
            .ToListAsync();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (userId == null)
            {
                return Unauthorized();
            }

            if (User.IsInRole("Freelancer"))
            {
                ViewBag.Recommendations = await _recommendationService
                    .GetRecommendedProjectsForFreelancerAsync(userId);
            }
            else if (User.IsInRole("Client"))
            {
                ViewBag.Recommendations = await _recommendationService
                    .GetRecommendedServicesForClientAsync(userId);
            }
        }
        
        return View(projects);
    }

    /// <summary>
    /// Отображает страницу политики конфиденциальности.
    /// </summary>
    /// <returns>Представление страницы политики конфиденциальности.</returns>
    public IActionResult Privacy()
    {
        return View();
    }

    /// <summary>
    /// Отображает страницу ошибки приложения.
    /// Используется системой обработки ошибок ASP.NET Core.
    /// </summary>
    /// <returns>Представление страницы ошибки с идентификатором запроса.</returns>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}