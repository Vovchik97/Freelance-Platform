using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.TaskTemplates;

/// <summary>
/// Модель страницы редактирования шаблона задач.
/// Позволяет администраторам изменять информацию о шаблоне,
/// связанные категории и список задач.
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
    public TaskTemplate Template { get; set; } = new();

    public List<Category> AllCategories { get; set; } = new();

    /// <summary>
    /// Загружает шаблон задачи по идентификатору
    /// вместе с категориями и элементами задач.
    /// </summary>
    /// <param name="id">Идентификатор шаблона задачи.</param>
    /// <returns>Страница редактирования или 404 если шаблон не найден.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Template = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (Template is null)
        {
            return NotFound();
        }

        AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Page();
    }

    /// <summary>
    /// Обновляет данные шаблона:
    /// основные поля, категории и список задач.
    /// </summary>
    /// <param name="categoryIds">Список идентификаторов выбранных категорий.</param>
    /// <param name="Items">Список задач для обновления шаблона.</param>
    /// <returns>Страница со списком шаблонов при успехе или текущая страница с ошибками.</returns>
    public async Task<IActionResult> OnPostAsync([FromForm] List<int> categoryIds, [FromForm] List<ItemInput> Items)
    {
        var template = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == Template.Id);

        if (template == null)
        {
            return NotFound();
        }
        
        template.Name = Template.Name;
        template.Description = Template.Description ?? string.Empty;
        
        await UpdateCategoriesAsync(template, categoryIds);
        UpdateItems(template, Items);

        try
        {
            await _context.SaveChangesAsync();
            TempData["Success"] = "Шаблон обновлен.";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Ошибка при обновлении: {ex.Message}");
            AllCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
            return Page();
        }
    }

    /// <summary>
    /// Заменяет категории шаблона на новый набор.
    /// Удаляет все текущие категории и добавляет выбранные активные.
    /// </summary>
    /// <param name="template">Шаблон для обновления категорий.</param>
    /// <param name="categoryIds">Список идентификаторов новых категорий.</param>
    private async Task UpdateCategoriesAsync(TaskTemplate template, List<int> categoryIds)
    {
        template.Categories.Clear();
        var categories = await _context.Categories
            .Where(c => categoryIds.Contains(c.Id) && c.IsActive)
            .ToListAsync();
        foreach (var cat in categories)
        {
            template.Categories.Add(cat);
        }
    }

    /// <summary>
    /// Заменяет список задач шаблона на новый набор.
    /// Удаляет все текущие задачи и добавляет новые, отсортированные по порядку.
    /// Пропускает задачи с пустым названием.
    /// </summary>
    /// <param name="template">Шаблон для обновления задач.</param>
    /// <param name="items">Список новых задач.</param>
    private void UpdateItems(TaskTemplate template, List<ItemInput>? items)
    {
        if (items == null)
        {
            return;
        }
        
        _context.TaskTemplateItems.RemoveRange(template.Items);
        template.Items.Clear();
        
        foreach (var item in items.OrderBy(i => i.OrderIndex))
        {
            if (string.IsNullOrWhiteSpace(item.Title))
            {
                continue;
            }

            template.Items.Add(new TaskTemplateItem
            {
                TaskTemplateId = template.Id,
                Title = item.Title,
                Description = item.Description ?? string.Empty,
                OrderIndex = item.OrderIndex
            });
        }
    }

    /// <summary>
    /// Данные одного элемента задачи,
    /// получаемые из формы редактирования шаблона.
    /// </summary>
    public class ItemInput
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
    }
}