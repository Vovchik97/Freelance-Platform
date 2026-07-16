using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Services;

/// <summary>
/// Модель страницы просмотра информации об услуге.
/// Позволяет администраторам просматривать данные услуги,
/// исполнителя и связанные отзывы.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Service? Service { get; set; }
    
    /// <summary>
    /// Загружает услугу по идентификатору,
    /// включая информацию об исполнителе и отзывы пользователей.
    /// </summary>
    /// <param name="id">Идентификатор услуги.</param>
    /// <returns>Страница с деталями услуги или 404 если услуга не найдена.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        Service = await _context.Services
            .Include(s => s.Freelancer)
            .Include(s => s.Reviews)
            .ThenInclude(r => r.User)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (Service == null)
        {
            return NotFound();
        }

        return Page();
    }

    /// <summary>
    /// Удаляет отзыв пользователя для выбранной услуги.
    /// </summary>
    /// <param name="reviewId">Идентификатор удаляемого отзыва.</param>
    /// <param name="serviceId">Идентификатор услуги (для редиректа).</param>
    /// <returns>Перенаправление на страницу деталей услуги или 404 если отзыв не найден.</returns>
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