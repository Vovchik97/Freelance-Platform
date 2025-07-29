using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

[Authorize]
public class ChatController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ChatController(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var chats = await _context.Chats
            .Include(c => c.Messages)
            .Where(c => c.ClientId == userId || c.FreelancerId == userId)
            .ToListAsync();
        
        // Создаем список с дополнительной информацией о чатах
        var chatViewModels = new List<ChatViewModel>();
        foreach (var chat in chats)
        {
            var otherUserId = chat.ClientId == userId ? chat.FreelancerId : chat.ClientId;
            var otherUser = await _userManager.FindByIdAsync(otherUserId);
        
            chatViewModels.Add(new ChatViewModel
            {
                Chat = chat,
                OtherUserName = otherUser!.UserName!,
                HasUnread = chat.Messages.Any(m => !m.IsRead && m.SenderId != userId)
            });
        }
        
        return View(chatViewModels);
    }

    public async Task<IActionResult> Chat(int chatId)
    {
        var chat = await _context.Chats
            .Include(c => c.Messages.OrderBy(m => m.SentAt))
            .FirstOrDefaultAsync(c => c.Id == chatId);

        var userId = _userManager.GetUserId(User);
        var otherUserId = chat?.ClientId == userId ? chat?.FreelancerId : chat?.ClientId;
        var otherUser = await _userManager.FindByIdAsync(otherUserId!);

        if (chat == null || (chat.ClientId != userId && chat.FreelancerId != userId))
        {
            return NotFound();
        }
        
        foreach (var msg in chat.Messages.Where(m => m.SenderId != userId && !m.IsRead))
        {
            msg.IsRead = true;
        }
        await _context.SaveChangesAsync();
        
        ViewBag.UserName = User.Identity!.Name;
        ViewBag.OtherUser = otherUser!.UserName;
        ViewBag.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return View(chat);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetUnreadChatsCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var unreadChatCount = await _context.Chats
            .Where(c => c.ClientId == userId || c.FreelancerId == userId)
            .Where(c => c.Messages.Any(m => m.SenderId != userId && !m.IsRead))
            .CountAsync();

        return Json(new { count = unreadChatCount });
    }
}