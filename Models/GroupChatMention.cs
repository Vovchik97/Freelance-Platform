namespace FreelancePlatform.Models;

public class GroupChatMention
{
    public int Id { get; set; }
    public int GroupChatMessageId { get; set; }
    public GroupChatMessage GroupChatMessage { get; set; } = null!;
    public string MentionedUserId { get; set; } = null!;
    public string MentionedUserName { get; set; } = null!;
}