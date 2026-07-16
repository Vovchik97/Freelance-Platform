using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Bids;

/// <summary>
/// Модель страницы просмотра подробной информации о заявке.
/// Используется администраторами для просмотра данных заявки,
/// связанного проекта и исполнителя.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Bid? Bid { get; set; }

    /// <summary>
    /// Загружает заявку по идентификатору
    /// и возвращает ее на страницу.
    /// </summary>
    /// <param name="id">Идентификатор заявки.</param>
    /// <returns>Страницу с подробной информацией о заявке.</returns>
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