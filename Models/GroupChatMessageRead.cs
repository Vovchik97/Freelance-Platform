namespace FreelancePlatform.Models;

public class GroupChatMessageRead
{
    public int Id { get; set; }
    public int MessageId { get; set; }
    public GroupChatMessage Message { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime ReadAt { get; set; } = DateTime.UtcNow;
}