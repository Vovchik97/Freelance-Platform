using System.Text;
using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

/// <summary>
/// Контроллер генерации XML-карты сайта.
/// Формирует sitemap.xml со статическими страницами,
/// открытыми проектами, услугами и публичными профилями пользователей.
/// </summary>
public class SitemapController : Controller
{
    private readonly AppDbContext _context;

    public SitemapController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Генерирует XML-карту сайта для поисковых систем.
    /// В карту включаются статические страницы,
    /// открытые проекты, доступные услуги и публичные профили пользователей.
    /// </summary>
    /// <returns>
    /// XML-документ sitemap.xml с типом содержимого
    /// <c>application/xml</c>.
    /// </returns>
    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Index()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sb = new StringBuilder();
        
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        var staticPages = new[]
        {
            ("", "1.0", "daily"),
            ("/Project", "0.9", "hourly"),
            ("/Service", "0.9", "hourly"),
        };

        foreach (var (path, priority, freq) in staticPages)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}{path}</loc>");
            sb.AppendLine($"    <changefreq>{freq}</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("  </url>");
        }

        var projects = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Open && !p.IsTeamProject)
            .Select(p => new { p.Id, p.CreatedAt })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        foreach (var project in projects)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}/Project/Details/{project.Id}</loc>");
            sb.AppendLine($"    <lastmod>{project.CreatedAt:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.8</priority>");
            sb.AppendLine("  </url>");
        }
        
        var services = await _context.Services
            .Where(s => s.Status == ServiceStatus.Available)
            .Select(s => new { s.Id, s.CreatedAt })
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
        
        foreach (var service in services)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}/Service/Details/{service.Id}</loc>");
            sb.AppendLine($"    <lastmod>{service.CreatedAt:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.7</priority>");
            sb.AppendLine("  </url>");
        }

        var userIds = await _context.Users
            .Select(u => u.Id)
            .ToListAsync();
        
        foreach (var userId in userIds)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}/PublicProfile/Public/{userId}</loc>");
            sb.AppendLine($"    <lastmod>{DateTime.UtcNow:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.6</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}