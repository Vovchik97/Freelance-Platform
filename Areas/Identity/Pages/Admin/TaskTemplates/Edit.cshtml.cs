using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.TaskTemplates;

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

    public List<Category> AllCategories { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Template = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (Template is null)
            return NotFound();

        AllCategories = await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync([FromForm] List<int> categoryIds, [FromForm] List<ItemInput> Items)
    {
        var template = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == Template.Id);

        if (template is null)
            return NotFound();

        // Обновляем основные поля
        template.Name = Template.Name;
        template.Description = Template.Description ?? string.Empty;

        // Обновляем категории
        template.Categories.Clear();
        var categories = await _context.Categories
            .Where(c => categoryIds.Contains(c.Id) && c.IsActive)
            .ToListAsync();
        foreach (var cat in categories)
        {
            template.Categories.Add(cat);
        }

        // Обновляем задачи
        _context.TaskTemplateItems.RemoveRange(template.Items);
        template.Items.Clear();

        if (Items != null && Items.Any())
        {
            foreach (var item in Items.OrderBy(i => i.OrderIndex))
            {
                if (!string.IsNullOrWhiteSpace(item.Title))
                {
                    template.Items.Add(new TaskTemplateItem
                    {
                        TaskTemplateId = template.Id,
                        Title = item.Title,
                        Description = item.Description ?? string.Empty,
                        OrderIndex = item.OrderIndex
                    });
                }
            }
        }

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

    public class ItemInput
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
    }
}