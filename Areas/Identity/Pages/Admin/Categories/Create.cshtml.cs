using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Categories;

/// <summary>
/// Модель страницы создания новой категории.
/// Позволяет администраторам добавлять категории
/// для классификации проектов и услуг платформы.
/// </summary>
[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty] 
    public string Name { get; set; } = string.Empty;
    
    [BindProperty]
    public string? Description { get; set; }
    
    [BindProperty]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Создаёт новую категорию после проверки корректности данных
    /// и отсутствия категории с таким же названием.
    /// </summary>
    /// <returns>Страница со списком категорий при успехе или текущая страница с ошибками.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError("Name", "Название обязательно");
            return Page();
        }

        var normalizedName = Name.Trim().ToLower();
        
        var exists = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == normalizedName);

        if (exists)
        {
            ModelState.AddModelError("Name", "Категория с таким названием уже существует");
            return Page();
        }

        var category = new Category
        {
            Name = Name,
            Description = Description,
            IsActive = IsActive,
            CreatedAt = DateTime.UtcNow
        };
        
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Категория \"{category.Name}\" создана";
        return RedirectToPage("Index");
    }
}