using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

public class ProjectController : Controller
{
    private readonly AppDbContext _context;

    public ProjectController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, string? status, decimal? minBudget, decimal? maxBudget, string sort)
    {
        var query = _context.Projects.Include(p => p.Client).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Title.Contains(search) || p.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProjectStatus>(status, out var parsedStatus))
        {
            query = query.Where(p => p.Status == parsedStatus);
        }

        if (minBudget.HasValue)
        {
            query = query.Where(p => p.Budget >= minBudget);    
        }

        if (maxBudget.HasValue)
        {
            query = query.Where(p => p.Budget <= maxBudget);
        }
        
        if (sort == "budget_desc")
            query = query.OrderByDescending(p => p.Budget);
        else if (sort == "budget_asc")
            query = query.OrderBy(p => p.Budget);
        else
            query = query.OrderByDescending(p => p.CreatedAt);
        
        var projects = await query.ToListAsync();

        ViewBag.Searcg = search;
        ViewBag.Status = status;
        ViewBag.MinBudget = minBudget;
        ViewBag.MaxBudget = maxBudget;
        ViewBag.Sort = sort;
        
        return View(projects);
    }
}