using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Bids;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context) => _context = context;

    public Bid? Bid { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Bid = await _context.Bids
            .Include(b => b.Project)
            .Include(b => b.Freelancer)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (Bid == null)
        {
            return NotFound();
        }

        return Page();
    }
}