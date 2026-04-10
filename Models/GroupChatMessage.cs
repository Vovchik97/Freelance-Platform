namespace FreelancePlatform.Models;

public class GroupChatMessage
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string SenderId { get; set; } = null!;
    public string SenderName { get; set; } = null!;
    public string Text { get; set; } = null!;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public List<GroupChatMention> Mentions { get; set; } = new();
    
    public string? AttachmentUrl { get; set; } 
    public string? AttachmentName { get; set; } 
    public string? AttachmentType { get; set; } 
    
    public int? ParentMessageId { get; set; }  
    public GroupChatMessage? ParentMessage { get; set; }
    public List<GroupChatMessage> Attachments { get; set; } = new List<GroupChatMessage>();

    public List<GroupChatMessageRead> ReadBy { get; set; } = new();
}