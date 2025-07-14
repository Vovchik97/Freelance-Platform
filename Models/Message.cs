namespace FreelancePlatform.Models;

public class Message
{
    public int Id { get; set; }
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public int? ProjectId { get; set; }
    public required string Content { get; set; }
    public DateTime SentAt { get; set; }
}