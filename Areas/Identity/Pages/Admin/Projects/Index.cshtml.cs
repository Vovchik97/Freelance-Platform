using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Projects;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Project> Projects { get; set; } = [];

    public async Task OnGetAsync()
    {
        Projects = await _context.Projects
            .Include(p => p.Client)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostOpenAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project is null) return NotFound();
        
        project.Status = ProjectStatus.Open;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project is null) return NotFound();

        project.Status = ProjectStatus.Cancelled;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var project = await _context.Projects.FindAsync(id);
        if (project is null) return NotFound();

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}