using System.Text.RegularExpressions;
using FreelancePlatform.Context;
using FreelancePlatform.Dto.Chat;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Hubs;

public class GroupChatHub : Hub
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ProjectActivityLogService _logService;

    public GroupChatHub(AppDbContext context, UserManager<IdentityUser> userManager,
        ProjectActivityLogService logService)
    {
        _context = context;
        _userManager = userManager;
        _logService = logService;
    }

    public async Task JoinProjectGroup(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project_{projectId}");
    }
    
    public async Task LeaveProjectGroup(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project_{projectId}");
    }

    public async Task SendGroupMessage(int projectId, string message, AttachmentDto[] attachments)
    {
        var user = await _userManager.GetUserAsync(Context.User!);
        
        if (user == null)
        {
            return;
        }
        
        var isMember = await _context.ProjectMembers
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == user.Id && m.Status == ProjectMemberStatus.Accepted);

        var project = await _context.Projects.FindAsync(projectId);
        
        if (project == null)
        {
            return;
        }

        var isClient = project.ClientId == user.Id;
        
        if (!isMember && !isClient)
        {
            return;
        }

        var mentionPattern = new Regex(@"@(\w+)");
        var matches = mentionPattern.Matches(message);

        var newMessage = new GroupChatMessage
        {
            ProjectId = projectId,
            SenderId = user.Id,
            SenderName = user.UserName!,
            Text = message ?? string.Empty,
            SentAt = DateTime.UtcNow
        };
        
        _context.GroupChatMessages.Add(newMessage);
        await _context.SaveChangesAsync();

        var mentionedUsers = new List<object>();

        foreach (Match match in matches)
        {
            var mentionedUserName = match.Groups[1].Value;
            var mentionedUser = await _userManager.FindByNameAsync(mentionedUserName);

            if (mentionedUser != null)
            {
                var mention = new GroupChatMention
                {
                    GroupChatMessageId = newMessage.Id,
                    MentionedUserId = mentionedUser.Id,
                    MentionedUserName = mentionedUser.UserName!
                };
                
                _context.GroupChatMentions.Add(mention);
                mentionedUsers.Add(new { userId = mentionedUser.Id, UserName = mentionedUser.UserName });
            }
        }

        if (attachments != null && attachments.Length > 0)
        {
            foreach (var att in attachments)
            {
                _context.GroupChatMessages.Add(new GroupChatMessage
                {
                    ProjectId = projectId,
                    SenderId = user.Id,
                    SenderName = user.UserName!,
                    Text = string.Empty,
                    SentAt = DateTime.UtcNow,
                    AttachmentUrl = att.Url,
                    AttachmentName = att.Name,
                    AttachmentType = att.Type,
                    ParentMessageId = newMessage.Id
                });
            }
        }

        await _context.SaveChangesAsync();

        var attachmentsDto = attachments ?? Array.Empty<AttachmentDto>();

        await Clients.Group($"project_{projectId}")
            .SendAsync("ReceiveGroupMessage",
                newMessage.Id,
                newMessage.SenderId,
                newMessage.SenderName,
                newMessage.Text,
                newMessage.SentAt,
                mentionedUsers,
                attachmentsDto);
    }

    public async Task UpdateTaskStatus(int projectId, int taskId, ProjectTaskStatus newStatus)
    {
        var user = await _userManager.GetUserAsync(Context.User!);

        if (user == null)
        {
            return;
        }

        var isMember = await _context.ProjectMembers
            .AnyAsync(m => m.ProjectId == projectId && m.UserId == user.Id && m.Status == ProjectMemberStatus.Accepted);

        var project = await _context.Projects.FindAsync(projectId);

        if (project == null)
        {
            return;
        }

        var isClient = project.ClientId == user.Id;

        if (!isMember && !isClient)
        {
            return;
        }

        var task = await _context.ProjectTasks
            .FirstOrDefaultAsync(t => t.Id == taskId && t.ProjectId == projectId);

        if (task == null)
        {
            return;
        }

        var oldStatus = task.Status;
        task.Status = newStatus;
        await _context.SaveChangesAsync();
        
        await _logService.LogAsync(
            projectId,
            $"Задача «{task.Title}» переведена из «{GetTaskStatusText(oldStatus)}» в «{GetTaskStatusText(newStatus)}»",
            user.Id,
            user.UserName
        );

        await Clients.Group($"project_{projectId}")
            .SendAsync("TaskStatusUpdated", taskId, (int)newStatus, GetTaskStatusText(newStatus));
    }

    private static string GetTaskStatusText(ProjectTaskStatus status) => status switch
    {
        ProjectTaskStatus.Todo => "К выполнению",
        ProjectTaskStatus.InProgress => "В процессе",
        ProjectTaskStatus.Done => "Выполнено",
        _ => "Неизвестно"
    };
}