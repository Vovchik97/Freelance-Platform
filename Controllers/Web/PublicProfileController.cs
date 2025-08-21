using FreelancePlatform.Context;
using FreelancePlatform.Dto.Profiles;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

public class PublicProfileController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    
    public PublicProfileController(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Public(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return NotFound();
        
        // всегда тянем пользователя
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var services = await _context.Services
            .Where(s => s.FreelancerId == userId)
            .Select(s => new ServiceInfoDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                Price = s.Price,
                Rating = null,
                FreelancerId = s.FreelancerId,
                Status = s.Status.ToString(),
                OrdersCount = s.Orders.Count
            })
            .ToListAsync();

        var dto = new PublicProfileDto
        {
            UserId = userId,
            UserName = user.UserName ?? "(Без имени)",
            AboutMe = profile?.AboutMe ?? string.Empty,
            Services = services
        };

        return View(dto);
    }


    // GET: /Profiles/My
    [Authorize(Roles = "Freelancer")]
    public async Task<IActionResult> My()
    {
        var user = await _userManager.GetUserAsync(User);
        var profile = await _context.UserProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == user!.Id);

        if (profile == null)
        {
            profile = new Models.UserProfile
            {
                UserId = user!.Id,
                User = user,
                AboutMe = ""
            };
            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Public", new { userId = user!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }
        
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.UserId == user.Id);
        
        return View(profile ?? new UserProfile { UserId = user.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(UserProfile model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }
        
        // подчищаем null → в пустую строку
        var aboutMe = model.AboutMe ?? string.Empty;
        
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(u => u.UserId == user.Id);

        if (profile == null)
        {
            profile = new UserProfile
            {
                UserId = user.Id,
                AboutMe = aboutMe
            };
            _context.UserProfiles.Add(profile);
        }
        else
        {
            profile.AboutMe = aboutMe;
            _context.UserProfiles.Update(profile);
        }
        
        await _context.SaveChangesAsync();
        return RedirectToAction("Public", new { userId = user.Id });
    }
}