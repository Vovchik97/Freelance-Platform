using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.TaskTemplates;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context) => _context = context;
    
    public TaskTemplate? Template { get; set; }

    public async Task OnGetAsync(int id)
    {
        Template = await _context.TaskTemplates
            .Include(t => t.Items)
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == id);
    }
}