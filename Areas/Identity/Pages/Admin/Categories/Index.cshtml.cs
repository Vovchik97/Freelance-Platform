using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Categories;

/// <summary>
/// Модель страницы управления категориями.
/// Позволяет администраторам просматривать категории,
/// изменять их статус активности и удалять неиспользуемые категории.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Category> Categories { get; set; } = new();
    public Dictionary<int, int> ProjectCounts { get; set; } = new();
    public Dictionary<int, int> ServiceCounts { get; set; } = new();

    /// <summary>
    /// Загружает список категорий и статистику их использования
    /// в проектах и услугах платформы.
    /// </summary>
    public async Task OnGetAsync()
    {
        Categories = await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();

        ProjectCounts = await _context.Projects
            .SelectMany(p => p.Categories)
            .GroupBy(c => c.Id)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
        
        ServiceCounts = await _context.Services
            .SelectMany(p => p.Categories)
            .GroupBy(c => c.Id)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
    }

    /// <summary>
    /// Изменяет статус активности категории.
    /// Активная категория доступна для использования,
    /// неактивная скрывается из выбора пользователей.
    /// </summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если категория не найдена.</returns>
    public async Task<IActionResult> OnPostToggleActiveAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }

        category.IsActive = !category.IsActive;
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = category.IsActive
            ? $"Категория \"{category.Name}\" активирована."
            : $"Категория \"{category.Name}\" деактивирована.";

        return RedirectToPage();
    }

    /// <summary>
    /// Удаляет категорию, если она не используется
    /// в проектах или услугах пользователей.
    /// </summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если категория не найдена.</returns>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Projects)
            .Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return NotFound();
        }

        if (category.Projects.Any() || category.Services.Any())
        {
            TempData["ErrorMessage"] = "Нельзя удалить категорию, которая используется. Деактивируйте её.";
            return RedirectToPage();
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = $"Категория \"{category.Name}\" удалена.";
        return RedirectToPage();
    }
}