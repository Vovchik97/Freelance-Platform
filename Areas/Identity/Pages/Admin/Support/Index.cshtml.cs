using FreelancePlatform.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Areas.Identity.Pages.Admin.Support;

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

        foreach (var chat in SupportChats)
        {
            var user = await _userManager.FindByIdAsync(chat.UserName);
            if (user != null)
            {
                chat.UserName = user.UserName ?? chat.UserName;
            }
        }
    }
    
    public class SupportChatViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Problem { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public bool IsBotActive { get; set; }
    }
}