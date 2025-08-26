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
            .Include(s => s.Reviews)
            .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (Service == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDeleteReviewAsync(int reviewId, int serviceId)
    {
        var review = await _context.Reviews.FindAsync(reviewId);
        if (review == null)
        {
            return NotFound();
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();
        
        return RedirectToPage(new { id = serviceId });
    }
}