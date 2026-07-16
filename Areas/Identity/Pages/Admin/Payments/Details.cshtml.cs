using FreelancePlatform.Context;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Payments;

/// <summary>
/// Модель страницы просмотра деталей платежа.
/// Позволяет администраторам просматривать информацию о платеже
/// и выполнять возврат средств при соблюдении условий.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly IBalanceService _balanceService;
    private readonly UserManager<IdentityUser> _userManager;
    
    public DetailsModel(AppDbContext context, IBalanceService balanceService, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _balanceService = balanceService;
        _userManager = userManager;
    }

    public Payment Payment { get; set; } = null!;
    public string? PayerEmail { get; set; }
    
    [TempData]
    public string ErrorMessage { get; set; }
    
    [TempData]
    public string? SuccessMessage { get; set; }

    /// <summary>
    /// Загружает информацию о платеже вместе со связанными данными
    /// и email пользователя, совершившего оплату.
    /// </summary>
    /// <param name="id">Идентификатор платежа.</param>
    /// <returns>Страница с деталями платежа или 404 если платёж не найден.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var payment = await _context.Payments
            .Include(p => p.Order)
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }

        Payment = payment;
        var user = await _userManager.FindByIdAsync(payment.PayerId);
        PayerEmail = user?.Email;
        return Page();
    }

    /// <summary>
    /// Выполняет возврат средств по платежу.
    /// Проверяет возможность возврата и вызывает соответствующую
    /// операцию балансного сервиса в зависимости от типа платежа.
    /// </summary>
    /// <param name="id">Идентификатор платежа.</param>
    /// <returns>Перенаправление на страницу деталей платежа или 404 если платёж не найден.</returns>
    public async Task<IActionResult> OnPostRefundAsync(int id)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }

        if (payment.Status == PaymentStatus.Refunded)
        {
            ErrorMessage = "Этот платеж уже был возвращен";
            return RedirectToPage(new { id });
        }

        if (payment.Status != PaymentStatus.Succeeded)
        {
            ErrorMessage = $"Возврат невозможен: статус платежа - {payment.Status}";
            return RedirectToPage(new { id });
        }

        var amount = payment.AmountMinor / 100m;

        if (payment.OrderId.HasValue)
        {
            await _balanceService.RefundForOrderAsync(payment.PayerId, amount, payment.OrderId.Value);
        }
        
        else if (payment.ProjectId.HasValue)
        {
            await _balanceService.RefundForProjectAsync(payment.PayerId, amount, payment.ProjectId.Value);
        }
        
        else if (payment.Type == PaymentType.Deposit)
        {
            await _balanceService.RefundDepositAsync(payment.PayerId, amount, payment.Id);
        }

        else
        {
            ErrorMessage = "Неизвестный тип платежа для возврата";
            return RedirectToPage(new { id });
        }
        
        payment.Status = PaymentStatus.Refunded;
        await _context.SaveChangesAsync();

        SuccessMessage = $"Средства ({amount} {payment.Currency}) возвращены";
        
        return RedirectToPage(new { id });
    }
}