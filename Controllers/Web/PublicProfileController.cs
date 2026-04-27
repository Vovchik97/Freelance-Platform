using FreelancePlatform.Context;
using FreelancePlatform.Dto.Profiles;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
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
    private readonly IReputationService _reputationService;
    private readonly IBlacklistService _blacklistService;
    
    public PublicProfileController(
        AppDbContext context, 
        UserManager<IdentityUser> userManager,
        IReputationService reputationService,
        IBlacklistService blacklistService)
    {
        _context = context;
        _userManager = userManager;
        _reputationService = reputationService;
        _blacklistService = blacklistService;
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

        var userRoles = await _userManager.GetRolesAsync(user);

        if (userRoles.Contains("Freelancer"))
        {
            return await FreelancerProfile(userId);
        }
        else if (userRoles.Contains("Client"))
        {
            return await ClientProfile(userId);
        }

        return NotFound();
    }
    
    private async Task<IActionResult> FreelancerProfile(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        var services = await _context.Services
            .Where(s => s.FreelancerId == userId)
            .Include(s => s.Reviews)
            .Include(s => s.Orders)
            .ToListAsync();

        var serviceDtos = services.Select(s => new ServiceInfoDto
        {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            Price = s.Price,
            FreelancerId = s.FreelancerId,
            Status = s.Status.ToString(),
            Reviews = s.Reviews.ToList(),
            OrdersCount = s.Orders.Count
        }).ToList();
        
        var allReviews = services.SelectMany(s => s.Reviews).ToList();

        var reputation = await _reputationService.GetAsync(userId);
        var reputationHistory = await _reputationService.GetHistoryAsync(userId);
        
        var currentUserId = _userManager.GetUserId(User);
        var isBlockedByMe = currentUserId != null && await _blacklistService.IsBlockedAsync(currentUserId, userId);
        
        var dto = new PublicProfileDto
        {
            UserId = userId,
            UserName = user.UserName ?? "(Без имени)",
            AboutMe = profile?.AboutMe ?? string.Empty,
            Services = serviceDtos,
            AverageRating    = allReviews.Any() ? allReviews.Average(r => r.Rating) : null,
            ReviewsCount     = allReviews.Count,
            AvgQuality       = allReviews.Any() ? allReviews.Average(r => r.QualityRating)       : null,
            AvgCommunication = allReviews.Any() ? allReviews.Average(r => r.CommunicationRating) : null,
            AvgDeadline      = allReviews.Any() ? allReviews.Average(r => r.DeadlineRating)      : null,
            AvgPrice         = allReviews.Any() ? allReviews.Average(r => r.PriceRating)         : null,
            ReputationScore = reputation.Score,
            ReputationHistory = reputationHistory.Take(5).ToList(),
            IsBlockedByMe = isBlockedByMe,
            IsFreelancer = true
        };

        return View(dto);
    }

    private async Task<IActionResult> ClientProfile(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        var projects = await _context.Projects
            .Where(p => p.ClientId == userId)
            .Include(p => p.Bids)
            .ToListAsync();
        
        var projectDtos = projects.Select(p => new ProjectInfoDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            Budget = p.Budget,
            ClientId = p.ClientId,
            Status = p.Status.ToString(),
            BidsCount = p.Bids.Count
        }).ToList();

        var reputation = await _reputationService.GetAsync(userId);
        var reputationHistory = await _reputationService.GetHistoryAsync(userId);
        
        var currentUserId = _userManager.GetUserId(User);
        var isBlockedByMe = currentUserId != null && await _blacklistService.IsBlockedAsync(currentUserId, userId);

        var dto = new PublicProfileDto
        {
            UserId = userId,
            UserName = user!.UserName ?? "(Без имени)",
            AboutMe = profile?.AboutMe ?? string.Empty,
            Services = new List<ServiceInfoDto>(),
            Projects = projectDtos,
            ReputationScore = reputation.Score,
            ReputationHistory = reputationHistory.Take(5).ToList(),
            IsBlockedByMe = isBlockedByMe,
            IsFreelancer = false
        };

        return View(dto);
    }
    
    [Authorize(Roles = "Freelancer, Client")]
    public async Task<IActionResult> My()
    {
        var user = await _userManager.GetUserAsync(User);
        var profile = await _context.UserProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == user!.Id);

        if (profile == null)
        {
            profile = new UserProfile
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