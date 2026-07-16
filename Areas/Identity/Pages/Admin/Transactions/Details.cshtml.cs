using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Transactions;

/// <summary>
/// Модель страницы просмотра подробной информации о транзакции.
/// Загружает данные операции и информацию о пользователе.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    
    public DetailsModel(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public BalanceTransaction? Transaction { get; set; }
    public string? UserEmail { get; set; }

    /// <summary>
    /// Загружает транзакцию по идентификатору
    /// и получает email пользователя, которому она принадлежит.
    /// </summary>
    /// <param name="id">Идентификатор транзакции.</param>
    /// <returns>Страница с деталями транзакции или 404 если транзакция не найдена.</returns>
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var transaction = await _context.BalanceTransactions
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return NotFound();
        }

        Transaction = transaction;
        var user = await _userManager.FindByIdAsync(transaction.UserId);
        UserEmail = user?.Email;
        return Page();
    }
}