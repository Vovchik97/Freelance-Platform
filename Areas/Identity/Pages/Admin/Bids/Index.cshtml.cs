using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Bids;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Bid> Bids { get; set; } = [];

    public async Task OnGetAsync()
    {
        Bids = await _context.Bids
            .Include(b => b.Project)
                .ThenInclude(p => p!.Client)
            .Include(b => b.Freelancer)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var bid = await _context.Bids.FindAsync(id);
        if (bid is null) return NotFound();

        _context.Bids.Remove(bid);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}