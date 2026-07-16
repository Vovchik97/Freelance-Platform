using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Users;

/// <summary>
/// Модель страницы просмотра информации о пользователе
/// в административной панели.
/// </summary>
[Authorize(Roles = "Admin")]
public class DetailsModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;

    public DetailsModel(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    public IdentityUser? SelectedUser { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// Загружает данные пользователя,
    /// его роли и статус блокировки.
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <returns>Страница с деталями пользователя или 404 если пользователь не найден.</returns>
    public async Task<IActionResult> OnGetAsync(string id)
    {
        SelectedUser = await _userManager.FindByIdAsync(id);

        if (SelectedUser == null)
        {
            return NotFound();
        }
        
        Roles = (await _userManager.GetRolesAsync(SelectedUser)).ToList();

        IsLockedOut = await _userManager.IsLockedOutAsync(SelectedUser);

        return Page();
    }
}