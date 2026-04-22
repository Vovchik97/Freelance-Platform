using System.ComponentModel.DataAnnotations;
using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.TaskTemplates;

[Authorize(Roles = "Admin")]
public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<Category> AllCategories { get; set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "Название обязательно")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<int> CategoryIds { get; set; } = new();

        public List<ItemInput> Items { get; set; } = new();
    }

    public class ItemInput
    {
        [Required(ErrorMessage = "Название задачи обязательно")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int OrderIndex { get; set; } = 1;
    }

    public async Task OnGetAsync()
    {
        AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync(List<int> categoryIds)
    {
        // Валидация названия
        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError("Input.Name", "Название обязательно");
            AllCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return Page();
        }

        // Получаем выбранные категории
        var categories = new List<Category>();
        if (categoryIds != null && categoryIds.Any())
        {
            categories = await _context.Categories
                .Where(c => categoryIds.Contains(c.Id) && c.IsActive)
                .ToListAsync();
        }

        // Создаем шаблон
        var template = new TaskTemplate
        {
            Name = Input.Name,
            Description = Input.Description ?? string.Empty,
            Categories = categories,
            Items = new List<TaskTemplateItem>()
        };

        // Добавляем задачи, если они есть
        if (Input.Items != null && Input.Items.Any())
        {
            foreach (var item in Input.Items.Where(i => !string.IsNullOrWhiteSpace(i.Title)))
            {
                template.Items.Add(new TaskTemplateItem
                {
                    Title = item.Title,
                    Description = item.Description ?? string.Empty,
                    OrderIndex = item.OrderIndex
                });
            }
        }

        // Проверяем, есть ли хотя бы одна задача
        if (!template.Items.Any())
        {
            ModelState.AddModelError(string.Empty, "Добавьте хотя бы одну задачу в шаблон");
            AllCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return Page();
        }

        try
        {
            _context.TaskTemplates.Add(template);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Шаблон \"{template.Name}\" создан успешно. Добавлено задач: {template.Items.Count}";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Ошибка при создании: {ex.Message}");
            AllCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return Page();
        }
    }
}