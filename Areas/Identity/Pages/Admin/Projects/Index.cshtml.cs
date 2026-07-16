using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Projects;

/// <summary>
/// Модель страницы управления проектами.
/// Позволяет администраторам просматривать проекты,
/// изменять их статус и удалять проекты из системы.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Project> Projects { get; set; } = new();

    /// <summary>
    /// Загружает список проектов вместе с данными клиентов
    /// и сортирует их по дате создания.
    /// </summary>
    public async Task OnGetAsync()
    {
        Projects = await _context.Projects
            .Include(p => p.Client)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Переводит проект в состояние открытого.
    /// Открытый проект доступен для дальнейшей работы пользователей.
    /// </summary>
    /// <param name="id">Идентификатор проекта.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если проект не найден.</returns>
    public async Task<IActionResult> OnPostOpenAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project is null)
        {
            return NotFound();
        }
        
        project.Status = ProjectStatus.Open;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    /// <summary>
    /// Закрывает проект, переводя его в статус отменённого.
    /// </summary>
    /// <param name="id">Идентификатор проекта.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если проект не найден.</returns>
    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project is null)
        {
            return NotFound();
        }

        project.Status = ProjectStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    /// <summary>
    /// Удаляет проект по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого проекта.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если проект не найден.</returns>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project is null)
        {
            return NotFound();
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}