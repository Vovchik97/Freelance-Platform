using System.ComponentModel.DataAnnotations;
using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.TaskTemplates;

/// <summary>
/// Модель страницы создания шаблона задач.
/// Позволяет администратору создать шаблон,
/// выбрать категории и добавить список задач.
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
    public InputModel Input { get; set; } = new();

    public List<Category> AllCategories { get; set; } = [];

    /// <summary>
    /// Данные формы создания шаблона задачи.
    /// </summary>
    public class InputModel
    {
        [Required(ErrorMessage = "Название обязательно")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public List<int> CategoryIds { get; set; } = new();

        public List<ItemInput> Items { get; set; } = new();
    }

    /// <summary>
    /// Данные одной задачи внутри шаблона.
    /// </summary>
    public class ItemInput
    {
        [Required(ErrorMessage = "Название задачи обязательно")]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int OrderIndex { get; set; } = 1;
    }

    /// <summary>
    /// Загружает активные категории для формы создания.
    /// </summary>
    public async Task OnGetAsync()
    {
        AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Создаёт новый шаблон задач с выбранными категориями и задачами.
    /// Проверяет обязательность названия и наличие хотя бы одной задачи.
    /// </summary>
    /// <param name="categoryIds">Список идентификаторов выбранных категорий.</param>
    /// <returns>Страница со списком шаблонов при успехе или текущая страница с ошибками.</returns>
    public async Task<IActionResult> OnPostAsync(List<int> categoryIds)
    {
        if (string.IsNullOrWhiteSpace(Input.Name))
        {
            ModelState.AddModelError("Input.Name", "Название обязательно");
            await LoadCategoriesAsync();
            return Page();
        }
        
        var template = new TaskTemplate
        {
            Name = Input.Name,
            Description = Input.Description ?? string.Empty,
            Categories = await GetCategoriesAsync(categoryIds),
        };

        AddItems(template);
        
        if (!template.Items.Any())
        {
            ModelState.AddModelError(string.Empty, "Добавьте хотя бы одну задачу в шаблон");
            await LoadCategoriesAsync();
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
            await LoadCategoriesAsync();
            return Page();
        }
    }

    /// <summary>
    /// Загружает активные категории для повторного отображения формы при ошибке.
    /// </summary>
    private async Task LoadCategoriesAsync()
    {
        AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Получает список категорий из БД по их идентификаторам.
    /// Возвращает только активные категории.
    /// </summary>
    /// <param name="categoryIds">Список идентификаторов для поиска.</param>
    /// <returns>Список найденных активных категорий.</returns>
    private async Task<List<Category>> GetCategoriesAsync(List<int> categoryIds)
    {
        if (categoryIds == null && !categoryIds.Any())
        {
            return new List<Category>();
        }
        
        return await _context.Categories
            .Where(c => categoryIds.Contains(c.Id) && c.IsActive)
            .ToListAsync();
    }

    /// <summary>
    /// Добавляет задачи из входных данных в шаблон.
    /// Пропускает задачи с пустым названием.
    /// </summary>
    /// <param name="template">Шаблон, в который добавляются задачи.</param>
    private void AddItems(TaskTemplate template)
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
}