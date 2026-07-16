using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.TaskTemplates;

/// <summary>
/// Модель страницы управления шаблонами задач.
/// Позволяет администраторам просматривать и удалять
/// шаблоны задач с их элементами.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    
    public IndexModel(AppDbContext context)
    {
        _context = context;
    }
    
    public List<TaskTemplate> Templates { get; set; } = new();

    /// <summary>
    /// Загружает список шаблонов задач
    /// вместе с элементами и категориями.
    /// </summary>
    public async Task OnGetAsync()
    {
        Templates = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Удаляет шаблон задачи и связанные с ним элементы.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого шаблона.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если шаблон не найден.</returns>
    public async Task<IActionResult> OnPostDelete(int id)
    {
        var template = await _context.TaskTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template is null)
        {
            return NotFound();
        }

        if (template.Items.Any())
        {
            _context.TaskTemplateItems.RemoveRange(template.Items);
        }
        
        _context.TaskTemplates.Remove(template);
        await _context.SaveChangesAsync();
        
        TempData["Success"] = "Шаблон удален.";
        return RedirectToPage();
    }
}