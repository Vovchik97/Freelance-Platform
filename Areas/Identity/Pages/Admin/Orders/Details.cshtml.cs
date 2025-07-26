using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Orders;

[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context) => _context = context;

    public Order? Order { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Order = await _context.Orders
            .Include(o => o.Service)
            .Include(o => o.Client)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (Order == null)
        {
            return NotFound();
        }

        return Page();
    }
}