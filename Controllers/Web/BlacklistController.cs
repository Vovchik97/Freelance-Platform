using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FreelancePlatform.Controllers.Web;

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

    [HttpGet]
    public async Task<IActionResult> My()
    {
        var userId = _userManager.GetUserId(User);
        var list = await _blacklistService.GetMyBlacklistAsync(userId);
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(string blockedId, string? reason, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == blockedId)
        {
            TempData["ErrorMessage"] = "Нельзя заблокировать самого себя";
            return Redirect(returnUrl ?? "/");
        }
        
        await _blacklistService.BlockAsync(userId, blockedId, reason);
        TempData["SuccessMessage"] = "Пользователь добавлен в чёрный список";

        return Redirect(returnUrl ?? "/");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(string blockedId, string? returnUrl)
    {
        var userId = _userManager.GetUserId(User);
        await _blacklistService.UnblockAsync(userId, blockedId);
        TempData["SuccessMessage"] = "Пользователь удалён из чёрного списка";

        return Redirect(returnUrl ?? Url.Action(nameof(My))!);
    }
}