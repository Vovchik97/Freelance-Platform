using FreelancePlatform.Context;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Hubs;

public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SmtpEmailSender _emailSender;

    public ChatHub(AppDbContext context, UserManager<IdentityUser> userManager, SmtpEmailSender emailSender)
    {
        _context = context;
        _userManager = userManager;
        _emailSender = emailSender;
    }
    
    public async Task SendMessage(int chatId, string userName, string message)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null) return;

        var chat = await _context.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == chatId);
        if (chat == null) return;
        
        var otherUserId = chat.FreelancerId == user.Id ? chat.ClientId : chat.FreelancerId;
        var otherUser = await _userManager.FindByIdAsync(otherUserId);

        var newMessage = new Message
        {
            ChatId = chatId,
            SenderId = user.Id,
            Text = message,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(newMessage);
        await _context.SaveChangesAsync();

        await Clients.Group(chatId.ToString())
            .SendAsync("ReceiveMessage", newMessage.SenderId, newMessage.Text, newMessage.SentAt);
        
        if (otherUser != null && !string.IsNullOrEmpty(otherUser.Email))
        {
            await _emailSender.SendEmailAsync(
                toEmail: otherUser.Email,
                subject: "Новое сообщение в чате",
                bodyHtml: $"Пользователь {userName} отправил новое сообщение."
            );
        }

    }
    
    public async Task SendAttachment(int chatId, string userName, string url, string name, string type)
    {
        var user = await _userManager.FindByNameAsync(userName);
        if (user == null) return;

        var chat = await _context.Chats
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == chatId);
        if (chat == null) return;

        var newMessage = new Message
        {
            ChatId = chatId,
            SenderId = user.Id,
            Text = "",
            AttachmentUrl = url,
            AttachmentName = name,
            AttachmentType = type,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(newMessage);
        await _context.SaveChangesAsync();

        await Clients.Group(chatId.ToString())
            .SendAsync("ReceiveMessage", newMessage.SenderId, "", newMessage.SentAt, new[]
            {
                new { url, name, type }
            });
    }
    
    public async Task JoinChat(string chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
    }
}