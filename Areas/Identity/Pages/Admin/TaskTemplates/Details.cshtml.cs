using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.TaskTemplates;

/// <summary>
/// Модель страницы просмотра подробной информации о шаблоне задачи.
/// Загружает шаблон вместе с категориями и элементами задач.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }
    
    public TaskTemplate? Template { get; set; }

    /// <summary>
    /// Загружает шаблон задачи по идентификатору
    /// вместе с категориями и элементами.
    /// </summary>
    /// <param name="id">Идентификатор шаблона задачи.</param>
    /// <returns>Страница с деталями шаблона или 404 если шаблон не найден.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Template = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (Template == null)
        {
            return NotFound();
        }
        
        return Page();
    }
}