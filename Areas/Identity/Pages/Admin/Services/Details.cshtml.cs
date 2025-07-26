using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Services;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context) => _context = context;

    public Service? Service { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Service = await _context.Services
            .Include(s => s.Freelancer) // загрузка данных фрилансера
            .FirstOrDefaultAsync(s => s.Id == id);

        if (Service == null)
        {
            return NotFound();
        }

        return Page();
    }
}