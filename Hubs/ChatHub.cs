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

        public ChatHub(AppDbContext context, UserManager<IdentityUser> userManager, SmtpEmailSender emailSender)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
        }

        public async Task SendMessage(int chatId, string userName, string message, AttachmentDto[] attachments)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return;

            var chat = await _context.Chats.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == chatId);
            if (chat == null) return;

            var newMessage = new Message
            {
                ChatId = chatId,
                SenderId = user.Id,
                Text = message ?? "",
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
                        Text = "",
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
                .SendAsync("ReceiveMessage", newMessage.SenderId, newMessage.Text, newMessage.SentAt, attachmentsDto);

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

        public async Task JoinChat(string chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
        }
    }
}
