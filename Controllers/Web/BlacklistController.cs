using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Controllers.Web;

/// <summary>
/// Контроллер управления чёрным списком пользователей.
/// Позволяет клиентам и исполнителям просматривать список блокировок,
/// добавлять пользователей в чёрный список и удалять их оттуда.
/// </summary>
[Authorize(Roles = "Client, Freelancer")]
public class BlacklistController : Controller
{
    private readonly IBlacklistService _blacklistService;
    private readonly UserManager<IdentityUser> _userManager;

    public BlacklistController(IBlacklistService blacklistService, UserManager<IdentityUser> userManager)
    {
        _blacklistService = blacklistService;
        _userManager = userManager;
    }

    /// <summary>
    /// Отображает список пользователей,
    /// добавленных текущим пользователем в чёрный список.
    /// </summary>
    /// <returns>
    /// Представление со списком заблокированных пользователей
    /// или ошибку авторизации, если пользователь не определён.
    /// </returns>
    [HttpGet]
    public async Task<IActionResult> My()
    {
        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }
        var list = await _blacklistService.GetMyBlacklistAsync(userId);
        return View(list);
    }

    /// <summary>
    /// Добавляет пользователя в чёрный список текущего пользователя.
    /// Запрещает блокировку самого себя.
    /// </summary>
    /// <param name="blockedId">Идентификатор пользователя, которого необходимо заблокировать.</param>
    /// <param name="reason">Причина добавления в чёрный список.</param>
    /// <param name="returnUrl">Адрес страницы для возврата после выполнения операции.</param>
    /// <returns>Перенаправление на указанную страницу или главную страницу приложения.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(string blockedId, string? reason, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Unauthorized();
        }
        
        if (userId == blockedId)
        {
            TempData["ErrorMessage"] = "Нельзя заблокировать самого себя";
            return Redirect(returnUrl ?? "/");
        }
        
        await _blacklistService.BlockAsync(userId, blockedId, reason);
        TempData["SuccessMessage"] = "Пользователь добавлен в чёрный список";

        return Redirect(returnUrl ?? "/");
    }

    /// <summary>
    /// Удаляет пользователя из чёрного списка текущего пользователя.
    /// </summary>
    /// <param name="blockedId">Идентификатор пользователя, которого необходимо удалить из списка блокировки.</param>
    /// <param name="returnUrl">Адрес страницы для возврата после выполнения операции.</param>
    /// <returns>Перенаправление на указанную страницу или страницу списка блокировок.</returns>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(string blockedId, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User);
        
        if (userId == null)
        {
            return Unauthorized();
        }
        
        await _blacklistService.UnblockAsync(userId, blockedId);
        TempData["SuccessMessage"] = "Пользователь удалён из чёрного списка";

        return Redirect(returnUrl ?? Url.Action(nameof(My))!);
    }
}