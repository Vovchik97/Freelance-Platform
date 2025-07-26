using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Projects;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context) => _context = context;

    public Project? Project { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == id);

        if (Project == null)
        {
            return NotFound();
        }

        return Page();
    }
}