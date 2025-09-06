namespace FreelancePlatform.Models;

public class ChatDto
{
    public required Chat Chat { get; set; }
    public required string OtherUserName { get; set; }
    public bool HasUnread { get; set; }
}