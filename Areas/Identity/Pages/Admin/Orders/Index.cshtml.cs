using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Orders;

/// <summary>
/// Модель страницы управления заказами.
/// Позволяет администраторам просматривать список заказов
/// и удалять записи заказов из системы.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Order> Orders { get; set; } = new();

    /// <summary>
    /// Загружает список заказов вместе со связанными данными:
    /// услугами, исполнителями и клиентами.
    /// </summary>
    public async Task OnGetAsync()
    {
        Orders = await _context.Orders
            .Include(o => o.Service)
                .ThenInclude(s => s!.Freelancer)
            .Include(o => o.Client)
            .ToListAsync();
    }
    
    /// <summary>
    /// Удаляет заказ по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого заказа.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если заказ не найден.</returns>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}