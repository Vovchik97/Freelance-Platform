using FreelancePlatform.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Support;

/// <summary>
/// Модель страницы управления обращениями в службу поддержки.
/// Отображает чаты пользователей, переданные от бота администраторам.
/// </summary>
[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public List<SupportChatViewModel> SupportChats { get; set; } = new();

    public IndexModel(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <summary>
    /// Загружает список обращений пользователей в поддержку
    /// и формирует данные для отображения в административной панели.
    /// </summary>
    public async Task OnGetAsync()
    {
        SupportChats = await _context.Chats
            .Where(c => c.IsSupport && !c.IsBotActive)
            .Include(c => c.Messages)
            .Select(c => new SupportChatViewModel
            {
                Id = c.Id,
                UserName = c.ClientId ?? c.FreelancerId ?? "Неизвестный пользователь",
                Problem = c.Messages
                    .Where(m => m.Id == c.LastEscalationMessageId)
                    .Select(m => m.Text)
                    .FirstOrDefault() ?? "-",
                LastUpdated = c.Messages
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => m.SentAt)
                    .FirstOrDefault(),
                IsBotActive = c.IsBotActive
            })
            .ToListAsync();

        await LoadUserNamesAsync();
    }

    /// <summary>
    /// Загружает имена пользователей по их идентификаторам.
    /// Заменяет временные ID на реальные имена пользователей.
    /// </summary>
    private async Task LoadUserNamesAsync()
    {
        foreach (var chat in SupportChats)
        {
            var user = await _userManager.FindByIdAsync(chat.UserName);
            if (user != null)
            {
                chat.UserName = user.UserName ?? chat.UserName;
            }
        }
    }
    
    /// <summary>
    /// Модель данных для отображения обращения пользователя
    /// в административном разделе поддержки.
    /// </summary>
    public class SupportChatViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Problem { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public bool IsBotActive { get; set; }
    }
}