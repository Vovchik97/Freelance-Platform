using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Services;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Service> Services { get; set; } = [];

    public async Task OnGetAsync()
    {
        Services = await _context.Services
            .Include(s => s.Freelancer)
            .ToListAsync();
    }
    
    public async Task<IActionResult> OnPostOpenAsync(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service is null) return NotFound();
        
        service.Status = ServiceStatus.Available;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
    
    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service is null) return NotFound();

        service.Status = ServiceStatus.Unavailable;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service is null) return NotFound();

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}