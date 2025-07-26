// Pages/Admin/Users/Index.cshtml.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        public class UserInfo
        {
            public required string Id { get; set; }
            public required string Email { get; set; }
            public List<string> Roles { get; set; } = new();
            public bool IsAdmin => Roles.Contains("Admin");
            
            public bool IsLockedOut { get; set; }
        }

        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public IndexModel(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public required List<UserInfo> Users { get; set; }

        public async Task OnGetAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            Users = new List<UserInfo>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var isLocked = await _userManager.IsLockedOutAsync(user);
                Users.Add(new UserInfo
                {
                    Id = user.Id,
                    Email = user.Email!,
                    Roles = roles.ToList(),
                    IsLockedOut = isLocked
                });
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
                await _userManager.DeleteAsync(user);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAdminAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                    await _userManager.RemoveFromRoleAsync(user, "Admin");
                else
                    await _userManager.AddToRoleAsync(user, "Admin");
            }
            return RedirectToPage();
        }

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
    }
}
