namespace FreelancePlatform.Models;

/// <summary>
/// Представляет упоминание пользователя в сообщении группового чата проекта.
/// </summary>
public class GroupChatMention
{
    public int Id { get; set; }
    public int GroupChatMessageId { get; set; }
    public GroupChatMessage GroupChatMessage { get; set; } = null!;
    public string MentionedUserId { get; set; } = null!;
    public string MentionedUserName { get; set; } = null!;
}