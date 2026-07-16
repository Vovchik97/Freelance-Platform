using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Payments;

/// <summary>
/// Модель страницы управления платежами.
/// Загружает список платежных операций администратора,
/// включая связанные заказы, проекты и информацию о плательщиках.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    
    public IndexModel(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public List<(Payment Payment, string? UserEmail)> Payments { get; set; } = new();

    /// <summary>
    /// Загружает список платежей вместе со связанными сущностями
    /// и добавляет информацию о пользователях, совершивших платежи.
    /// </summary>
    public async Task OnGetAsync()
    {
        var payments = await _context.Payments
            .Include(p => p.Order)
            .Include(p => p.Project)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var payerIds = payments.Select(p => p.PayerId).Distinct().ToList();
        var users = await _userManager.Users
            .Where(u => payerIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);

        Payments = payments.Select(p => (p, users.GetValueOrDefault(p.PayerId))).ToList();
    }
}