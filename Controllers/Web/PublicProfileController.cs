using FreelancePlatform.Context;
using FreelancePlatform.Dto.Profiles;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

/// <summary>
/// Контроллер управления публичными профилями пользователей.
/// Отвечает за отображение профилей клиентов и исполнителей,
/// просмотр информации о деятельности пользователей,
/// редактирование собственного профиля и работу с репутацией.
/// </summary>
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

    /// <summary>
    /// Отображает публичный профиль пользователя.
    /// Определяет тип пользователя по роли и загружает соответствующий профиль исполнителя или клиента.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя, чей профиль необходимо открыть.</param>
    /// <returns> Представление публичного профиля пользователя
    /// или результат с ошибкой, если пользователь не найден или его роль не поддерживается.
    /// </returns>
    [AllowAnonymous]
    public async Task<IActionResult> Public(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return NotFound();
        
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound();

        if (await _userManager.IsInRoleAsync(user, "Freelancer"))
        {
            return await GetFreelancerProfileAsync(user);
        }
        
        if (await _userManager.IsInRoleAsync(user, "Client"))
        {
            return await GetClientProfileAsync(user);
        }

        return NotFound();
    }
    
    /// <summary>
    /// Формирует публичный профиль исполнителя.
    /// Загружает список услуг, отзывы, рейтинг, историю репутации и информацию о блокировке пользователя.
    /// </summary>
    /// <param name="user">Пользователь, для которого формируется профиль исполнителя.</param>
    /// <returns>Представление с данными публичного профиля исполнителя.</returns>
    private async Task<IActionResult> GetFreelancerProfileAsync(IdentityUser user)
    {
        var userId = user.Id;
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

        var hasReviews = allReviews.Any();
        
        var dto = new PublicProfileDto
        {
            UserId = userId,
            UserName = user.UserName ?? "(Без имени)",
            AboutMe = profile?.AboutMe ?? string.Empty,
            Services = serviceDtos,
            AverageRating    = hasReviews ? allReviews.Average(r => r.Rating) : null,
            ReviewsCount     = allReviews.Count,
            AvgQuality       = hasReviews ? allReviews.Average(r => r.QualityRating)       : null,
            AvgCommunication = hasReviews ? allReviews.Average(r => r.CommunicationRating) : null,
            AvgDeadline      = hasReviews ? allReviews.Average(r => r.DeadlineRating)      : null,
            AvgPrice         = hasReviews ? allReviews.Average(r => r.PriceRating)         : null,
            ReputationScore = reputation.Score,
            ReputationHistory = reputationHistory.Take(5).ToList(),
            IsBlockedByMe = isBlockedByMe,
            IsFreelancer = true
        };

        return View(dto);
    }

    /// <summary>
    /// Формирует публичный профиль клиента.
    /// Загружает опубликованные проекты клиента,информацию о репутации и статус блокировки пользователя.
    /// </summary>
    /// <param name="user">Пользователь, для которого формируется профиль клиента.</param>
    /// <returns>Представление с данными публичного профиля клиента.</returns>
    private async Task<IActionResult> GetClientProfileAsync(IdentityUser user)
    {
        var userId = user.Id;
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
    
    /// <summary>
    /// Открывает профиль текущего пользователя.
    /// Если профиль ещё не создан, создаёт пустой профиль пользователя.
    /// </summary>
    /// <returns>Перенаправление на страницу публичного профиля пользователя.</returns>
    [Authorize(Roles = "Freelancer, Client")]
    public async Task<IActionResult> My()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Unauthorized();
        }
        
        var profile = await _context.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

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

    /// <summary>
    /// Отображает форму редактирования информации профиля текущего пользователя.
    /// </summary>
    /// <returns>Представление формы редактирования профиля или ошибку авторизации, если пользователь не найден.</returns>
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

    /// <summary>
    /// Сохраняет изменения информации профиля пользователя.
    /// Создаёт профиль автоматически, если он отсутствует.
    /// </summary>
    /// <param name="model">Модель профиля с обновлёнными данными пользователя.</param>
    /// <returns>Перенаправление на публичный профиль пользователя после успешного сохранения.</returns>
    [HttpPost]
    public async Task<IActionResult> Edit(UserProfile model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Unauthorized();
        }
        
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
        }
        
        await _context.SaveChangesAsync();
        return RedirectToAction("Public", new { userId = user.Id });
    }
}