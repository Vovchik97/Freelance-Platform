using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Projects;

/// <summary>
/// Модель страницы просмотра деталей проекта.
/// Позволяет администраторам просматривать информацию о проекте,
/// клиенте и участниках проекта.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context) => _context = context;

    public Project? Project { get; set; }

    /// <summary>
    /// Загружает проект по идентификатору вместе со связанными данными:
    /// клиентом и участниками проекта.
    /// </summary>
    /// <param name="id">Идентификатор проекта.</param>
    /// <returns>Страница с деталями проекта или 404 если проект не найден.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Project = await _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (Project == null)
        {
            return NotFound();
        }

        return Page();
    }
}