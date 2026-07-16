using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Bids;

/// <summary>
/// Модель страницы администрирования заявок на проекты.
/// Позволяет просматривать и удалять заявки пользователей.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Bid> Bids { get; set; } = new();

    /// <summary>
    /// Загружает список заявок вместе с информацией
    /// о проекте, заказчике и исполнителе.
    /// </summary>
    public async Task OnGetAsync()
    {
        Bids = await _context.Bids
            .Include(b => b.Project)
                .ThenInclude(p => p!.Client)
            .Include(b => b.Freelancer)
            .ToListAsync();
    }

    /// <summary>
    /// Удаляет заявку из системы.
    /// </summary>
    /// <param name="id">Идентификатор удаляемой заявки.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если заявка не найдена.</returns>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var bid = await _context.Bids.FindAsync(id);
        if (bid is null)
        {
            return NotFound();
        }

        _context.Bids.Remove(bid);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}