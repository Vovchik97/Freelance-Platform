using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Hubs;

public class ChatHub : Hub
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;

    public ChatHub(AppDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }
    
    public async Task SendMessage(int chatId, string userName, string message)
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
            Text = message,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(newMessage);
        await _context.SaveChangesAsync();

        await Clients.Group(chatId.ToString())
            .SendAsync("ReceiveMessage", newMessage.SenderId, newMessage.Text, newMessage.SentAt);
    }
    
    public async Task JoinChat(string chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
    }
}