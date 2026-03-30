using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Categories;

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

    public void OnGet()
    {
        
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ModelState.AddModelError("Name", "Название обязательно");
            return Page();
        }
        
        var exists = await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == Name.ToLower());

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