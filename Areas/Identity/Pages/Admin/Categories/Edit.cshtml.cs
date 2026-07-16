using FreelancePlatform.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Categories;

/// <summary>
/// Модель страницы редактирования категории.
/// Позволяет администраторам изменять данные существующей категории.
/// </summary>
[Authorize(Roles = "Admin")]
public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public bool IsActive { get; set; }

    /// <summary>
    /// Загружает данные категории по идентификатору
    /// для заполнения формы редактирования.
    /// </summary>
    /// <param name="id">Идентификатор категории.</param>
    /// <returns>Страница редактирования или 404 если категория не найдена.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var category = await _context.Categories.FindAsync(id);
        if (category == null)
        {
            return NotFound();
        }
        
        Id = category.Id;
        Name = category.Name;
        Description = category.Description;
        IsActive = category.IsActive;

        return Page();
    }
    
    /// <summary>
    /// Обновляет данные категории после проверки корректности
    /// и отсутствия дубликатов названий.
    /// </summary>
    /// <returns>Страница со списком категорий при успехе или текущая страница с ошибками.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError("Name", "Название обязательно");
            return Page();
        }

        var category = await _context.Categories.FindAsync(Id);
        if (category == null)
        {
            return NotFound();
        }

        var normalizedName = Name.Trim().ToLower();
        
        var duplicate = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == normalizedName && c.Id != Id);

        if (duplicate)
        {
            ModelState.AddModelError("Name", "Категория с таким названием уже существует");
            return Page();
        }
        
        category.Name = Name.Trim();
        category.Description = Description;
        category.IsActive = IsActive;
        
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = $"Категория \"{category.Name}\" обновлена";
        return RedirectToPage("Index");
    }
}