using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Orders;

/// <summary>
/// Модель страницы просмотра деталей заказа.
/// Позволяет администраторам просматривать информацию
/// о выбранном заказе и связанных с ним данных.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;

    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public Order? Order { get; set; }

    /// <summary>
    /// Загружает информацию о заказе по идентификатору
    /// вместе со связанными данными клиента и услуги.
    /// </summary>
    /// <param name="id">Идентификатор заказа.</param>
    /// <returns>Страница с деталями заказа или 404 если заказ не найден.</returns>
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