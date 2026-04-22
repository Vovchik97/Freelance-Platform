using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.TaskTemplates;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    
    public IndexModel(AppDbContext context)
    {
        _context = context;
    }
    
    public List<TaskTemplate> Templates { get; set; } = [];

    public async Task OnGetAsync()
    {
        Templates = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

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