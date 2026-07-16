using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Services;

/// <summary>
/// Модель страницы управления услугами.
/// Позволяет администраторам просматривать услуги,
/// изменять их статус и удалять услуги платформы.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Service> Services { get; set; } = new();

    /// <summary>
    /// Загружает список услуг вместе с информацией
    /// об исполнителях, которые их предоставляют.
    /// </summary>
    public async Task OnGetAsync()
    {
        Services = await _context.Services
            .Include(s => s.Freelancer)
            .ToListAsync();
    }
    
    /// <summary>
    /// Переводит услугу в состояние доступной
    /// для отображения пользователям.
    /// </summary>
    /// <param name="id">Идентификатор услуги.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если услуга не найдена.</returns>
    public async Task<IActionResult> OnPostOpenAsync(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }
        
        service.Status = ServiceStatus.Available;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
    
    /// <summary>
    /// Переводит услугу в состояние недоступной
    /// для временного скрытия услуги на платформе.
    /// </summary>
    /// <param name="id">Идентификатор услуги.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если услуга не найдена.</returns>
    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }

        service.Status = ServiceStatus.Unavailable;
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    /// <summary>
    /// Удаляет услугу из системы по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор удаляемой услуги.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если услуга не найдена.</returns>
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service == null)
        {
            return NotFound();
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();
        return RedirectToPage();
    }
}