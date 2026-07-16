using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Users;

/// <summary>
/// Модель страницы управления пользователями.
/// Позволяет администраторам просматривать пользователей,
/// изменять роли и управлять блокировкой аккаунтов.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly UserManager<IdentityUser> _userManager;

    public IndexModel(UserManager<IdentityUser> userManager)
    { 
        _userManager = userManager;
    }

    public required List<UserInfo> Users { get; set; } = new();

    /// <summary>
    /// Загружает список пользователей,
    /// их роли и статус блокировки.
    /// </summary>
    public async Task OnGetAsync()
    { 
        var users = await _userManager.Users.ToListAsync();
        Users = new List<UserInfo>();
        
        foreach (var user in users)
        { 
            Users.Add(await MapUserAsync(user));
        }
    }

    /// <summary>
    /// Удаляет пользователя из системы.
    /// </summary>
    /// <param name="id">Идентификатор удаляемого пользователя.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если пользователь не найден.</returns>
    public async Task<IActionResult> OnPostDeleteAsync(string id)
    { 
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        { 
            return NotFound();
        }
            
        await _userManager.DeleteAsync(user);
            
        return RedirectToPage();
    }

    /// <summary>
    /// Добавляет или удаляет у пользователя роль администратора.
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если пользователь не найден.</returns>
    public async Task<IActionResult> OnPostToggleAdminAsync(string id)
    { 
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) 
        { 
            return NotFound();
        } 
        var isAdmin = await _userManager.IsInRoleAsync(user, "Admin"); 
        if (isAdmin) 
        { 
            await _userManager.RemoveFromRoleAsync(user, "Admin");
        }
        else 
        { 
            await _userManager.AddToRoleAsync(user, "Admin");
        } 
        return RedirectToPage();
    }
    
    /// <summary>
    /// Блокирует или разблокирует пользователя.
    /// </summary>
    /// <param name="id">Идентификатор пользователя.</param>
    /// <returns>Перенаправление на текущую страницу или 404 если пользователь не найден.</returns>
    public async Task<IActionResult> OnPostToggleBanAsync(string id) 
    { 
        var user = await _userManager.FindByIdAsync(id); 
        if (user == null) 
        { 
            return NotFound();
        }
        var isLocked = await _userManager.IsLockedOutAsync(user); 
        if (isLocked) 
        { 
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
        }
        else 
        { 
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        }
        
        return RedirectToPage();
    }
    
    /// <summary>
    /// Преобразует IdentityUser в модель UserInfo для отображения.
    /// Загружает роли и статус блокировки для пользователя.
    /// </summary>
    /// <param name="user">Пользователь из Identity.</param>
    /// <returns>Модель с данными пользователя для отображения.</returns>
    private async Task<UserInfo> MapUserAsync(IdentityUser user) 
    { 
        var roles = await _userManager.GetRolesAsync(user); 
        return new UserInfo 
        { 
            Id = user.Id, 
            Email = user.Email ?? string.Empty, 
            Roles = roles.ToList(), 
            IsLockedOut = await _userManager.IsLockedOutAsync(user)
        };
    }
}

/// <summary>
/// Данные пользователя для отображения в административной панели.
/// </summary>
public class UserInfo
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public bool IsAdmin => Roles.Contains("Admin");
            
    public bool IsLockedOut { get; set; }
}
