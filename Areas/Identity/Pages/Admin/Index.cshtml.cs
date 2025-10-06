using FreelancePlatform.Context;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }
    public async Task OnGetAsync()
    {
        var HasNewSupport = await _context.Chats.AnyAsync(c => c.IsSupport && !c.IsBotActive);
        ViewData["HasNewSupport"] = HasNewSupport;
    }
}