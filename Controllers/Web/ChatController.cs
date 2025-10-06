using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Hubs;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
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
    private readonly SupportBotService _botService;

    public ChatController(AppDbContext context, UserManager<IdentityUser> userManager, SupportBotService botService)
    {
        _context = context;
        _userManager = userManager;
        _botService = botService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        
        var supportChat = await _context.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.IsSupport && c.ClientId == userId);

        if (supportChat == null)
        {
            supportChat = new Chat
            {
                IsSupport = true,
                IsBotActive = true,
                ClientId = userId!,
                FreelancerId = userId!, // placeholder: в UI будем показывать "Техподдержка"
            };
            _context.Chats.Add(supportChat);
            await _context.SaveChangesAsync();

            var (reply, escalate) = _botService.GetReply("");
            var botMessage = new Message
            {
                ChatId = supportChat.Id,
                SenderId = ChatHub.BotSenderId,
                SenderName = "Бот поддержки",
                Text = reply,
                SentAt = DateTime.UtcNow
            };
            _context.Messages.Add(botMessage);
            await _context.SaveChangesAsync();
        }

        var chats = await _context.Chats
            .Include(c => c.Messages)
            .Where(c => c.ClientId == userId || c.FreelancerId == userId)
            .ToListAsync();
        
        // Создаем список с дополнительной информацией о чатах
        var chatViewModels = new List<ChatDto>();
        foreach (var chat in chats)
        {
            string otherUserName;
            if (chat.IsSupport)
            {
                otherUserName = "Техподдержка";
            }
            else
            {
                var otherUserId = chat.ClientId == userId ? chat.FreelancerId : chat.ClientId;
                var otherUser = await _userManager.FindByIdAsync(otherUserId);
                otherUserName = otherUser!.UserName ?? "Пользователь";
            }
        
            chatViewModels.Add(new ChatDto
            {
                Chat = chat,
                OtherUserName = otherUserName,
                HasUnread = chat.Messages.Any(m => !m.IsRead && m.SenderId != userId)
            });
        }
        
        return View(chatViewModels);
    }

    public async Task<IActionResult> Chat(int chatId)
    {
        var chat = await _context.Chats
            .Include(c => c.Messages.Where(m => m.ParentMessageId == null))
            .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(c => c.Id == chatId);

        if (chat == null)
        {
            return NotFound();
        }

        var userId = _userManager.GetUserId(User);

        if (chat.ClientId != userId && chat.FreelancerId != userId && !chat.IsSupport)
        {
            return NotFound();
        }
        
        foreach (var msg in chat.Messages.Where(m => m.SenderId != userId && !m.IsRead))
        {
            msg.IsRead = true;
        }
        await _context.SaveChangesAsync();
        
        ViewBag.UserName = User.Identity!.Name;
        ViewBag.OtherUser = chat.IsSupport ? "Техподдержка" : (chat.ClientId == userId ? (await _userManager.FindByIdAsync(chat.FreelancerId))?.UserName : (await _userManager.FindByIdAsync(chat.ClientId))?.UserName);
        ViewBag.CurrentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        ViewBag.IsSupport = chat.IsSupport;
        ViewBag.IsBotActive = chat.IsBotActive;
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
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadAttachment(List<IFormFile> files)
    {
        var userId = _userManager.GetUserId(User);
        if (files == null || files.Count == 0)
            return BadRequest();

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", userId!);
        Directory.CreateDirectory(uploadsFolder);

        var fileResults = new List<object>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            var fileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            fileResults.Add(new
            {
                url = $"/uploads/{userId}/{fileName}",
                name = file.FileName,
                type = file.ContentType
            });
        }

        // Сериализуем в чистый JSON без $id, $values
        var json = System.Text.Json.JsonSerializer.Serialize(fileResults);
        return Content(json, "application/json");
    }
}