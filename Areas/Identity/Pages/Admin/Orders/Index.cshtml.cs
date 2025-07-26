using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Orders;
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Order> Orders { get; set; } = [];

    public async Task OnGetAsync()
    {
        Orders = await _context.Orders
            .Include(o => o.Service)
                .ThenInclude(s => s!.Freelancer)
            .Include(o => o.Client)
            .ToListAsync();
    }
    
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null) return NotFound();

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}