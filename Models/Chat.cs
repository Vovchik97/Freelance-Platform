namespace FreelancePlatform.Models;

public class Chat
{
    public int Id { get; set; }
    public required string ClientId { get; set; }
    public required string FreelancerId { get; set; }
    public required List<Message> Messages { get; set; }

    public bool IsRead { get; set; } = false;

}   