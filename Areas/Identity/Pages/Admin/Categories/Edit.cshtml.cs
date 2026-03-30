using FreelancePlatform.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Categories;

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

        var duplicate = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == Name.ToLower() && c.Id != Id);

        if (duplicate)
        {
            ModelState.AddModelError("Name", "Категория с таким названием уже существует");
            return Page();
        }
        
        category.Name = Name;
        category.Description = Description;
        category.IsActive = IsActive;
        
        await _context.SaveChangesAsync();
        
        TempData["SuccessMessage"] = $"Категория \"{category.Name}\" обновлена";
        return RedirectToPage("Index");
    }
}