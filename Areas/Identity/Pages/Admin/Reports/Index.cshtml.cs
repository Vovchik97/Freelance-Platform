using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Reports;

/// <summary>
/// Модель страницы управления жалобами.
/// Позволяет администраторам просматривать список пользовательских жалоб
/// и связанную с ними информацию о заказах и проектах.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Report> Reports { get; set; } = new();

    /// <summary>
    /// Загружает список жалоб вместе со связанными заказами
    /// и проектами, сортируя их по статусу и дате создания.
    /// </summary>
    public async Task OnGetAsync()
    {
        Reports = await _context.Reports
            .Include(r => r.Order)
            .Include(r => r.Project)
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}