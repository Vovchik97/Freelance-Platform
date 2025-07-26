using FreelancePlatform.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Users;

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

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Console.WriteLine("Получен id: " + id); // Временный вывод в консоль
        SelectedUser = await _userManager.FindByIdAsync(id);

        if (SelectedUser == null)
        {
            return NotFound();
        }
        
        Roles = (await _userManager.GetRolesAsync(SelectedUser)).ToList();
        
        IsLockedOut = await _userManager.IsLockedOutAsync(SelectedUser);
        Console.WriteLine($"User found: {SelectedUser?.UserName}, {SelectedUser?.Email}, {SelectedUser?.PhoneNumber}");

        return Page();
    }
}