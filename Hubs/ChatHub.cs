using FreelancePlatform.Context;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using FreelancePlatform.Dto.Chat;

namespace FreelancePlatform.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SmtpEmailSender _emailSender;
        private readonly SupportBotService _botService;
        
        public const string BotSenderId = "BOT";

        public ChatHub(AppDbContext context, UserManager<IdentityUser> userManager, SmtpEmailSender emailSender, SupportBotService botService)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _botService = botService;
        }

        public async Task SendMessage(int chatId, string userName, string message, AttachmentDto[] attachments)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return;

            var chat = await _context.Chats
                .Include(c => c.Messages)
                .ThenInclude(m => m.Attachments)
                .FirstOrDefaultAsync(c => c.Id == chatId);
            
            if (chat == null) return;

            var newMessage = new Message
            {
                ChatId = chatId,
                SenderId = user.Id,
                SenderName = user.UserName,
                Text = message ?? string.Empty,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(newMessage);
            await _context.SaveChangesAsync();

            if (attachments != null && attachments.Length > 0)
            {
                foreach (var att in attachments)
                {
                    var attachmentMessage = new Message
                    {
                        ChatId = chatId,
                        SenderId = user.Id,
                        SenderName = user.UserName,
                        Text = String.Empty,
                        AttachmentUrl = att.Url,
                        AttachmentName = att.Name,
                        AttachmentType = att.Type,
                        SentAt = DateTime.UtcNow,
                        ParentMessageId = newMessage.Id
                    };
                    _context.Messages.Add(attachmentMessage);
                }
                await _context.SaveChangesAsync();
            }

            // Формируем DTO с вложениями для клиента
            var attachmentsDto = attachments ?? new AttachmentDto[0];

            await Clients.Group(chatId.ToString())
                .SendAsync("ReceiveMessage", newMessage.SenderId, newMessage.SenderName, newMessage.Text, newMessage.SentAt, attachmentsDto);

            if (!chat.IsSupport)
            {
                var otherUserId = chat.FreelancerId == user.Id ? chat.ClientId : chat.FreelancerId;
                var otherUser = await _userManager.FindByIdAsync(otherUserId);
                if (otherUser != null && !string.IsNullOrEmpty(otherUser.Email))
                {
                    await _emailSender.SendEmailAsync(
                        toEmail: otherUser.Email,
                        subject: "Новое сообщение в чате",
                        bodyHtml: $"Пользователь {userName} отправил новое сообщение."
                    );
                }
            }

            if (chat.IsSupport && chat.IsBotActive)
            {
                var (reply, escalate) = _botService.GetReply(message ?? string.Empty);

                var botMsg = new Message
                {
                    ChatId = chatId,
                    SenderId = BotSenderId,
                    SenderName = "Бот поддержки",
                    Text = reply,
                    SentAt = DateTime.UtcNow
                };
                _context.Messages.Add(botMsg);
                await _context.SaveChangesAsync();

                await Clients.Group(chatId.ToString())
                    .SendAsync("ReceiveMessage", botMsg.SenderId, botMsg.SenderName, botMsg.Text, botMsg.SentAt, new AttachmentDto[0]);
                
                Message escalationMessage = null;
                
                if (escalate)
                {
                    escalationMessage = await _context.Messages
                        .Where(m => m.ChatId == chatId && (m.SenderId == chat.ClientId || m.SenderId == chat.FreelancerId) && m.Id != newMessage.Id)
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefaultAsync();

                    if (escalationMessage != null)
                    {
                        chat.LastEscalationMessageId = escalationMessage.Id;
                    }
                    
                    chat.IsBotActive = false;
                    await _context.SaveChangesAsync();

                    await Clients.Group("Admins").SendAsync("SupportRequest", chatId, user.Id);
                }
            }
        }

        public async Task JoinChat(string chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
        }

        public async Task JoinAdminsGroup()
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null)
            {
                return;
            }
            
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }
        }

        public async Task LeaveAdminsGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
        }

        public async Task AdminTakeChat(int chatId)
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null)
            {
                return;
            }
            
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
            {
                return;
            }

            var chat = await _context.Chats.FindAsync(chatId);
            if (chat == null)
            {
                return;
            }

            chat.IsBotActive = false;
            await _context.SaveChangesAsync();

            await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString());

            await Clients.Group(chatId.ToString()).SendAsync("AdminJoined", user.Id, user.UserName);
        }

        public async Task AdminLeaveChat(int chatId)
        {
            var user = await _userManager.GetUserAsync(Context.User!);
            if (user == null)
            {
                return;
            }
            
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Admin"))
            {
                return;
            }
            
            var chat = await _context.Chats.FindAsync(chatId);
            if (chat == null)
            {
                return;
            }

            chat.IsBotActive = true;
            await _context.SaveChangesAsync();

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString());

            await Clients.Group(chatId.ToString()).SendAsync("AdminLeft", user.Id, user.UserName);
        }
    }
}