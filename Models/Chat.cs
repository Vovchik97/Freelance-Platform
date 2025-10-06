namespace FreelancePlatform.Models;

public class Chat
{
    public int Id { get; set; }
    public string ClientId { get; set; }
    public string FreelancerId { get; set; }
    public List<Message> Messages { get; set; }

    public bool IsRead { get; set; } = false;
    
    public bool IsSupport { get; set; } = false;
    
    public bool IsBotActive { get; set; } = false;
    
    public int LastEscalationMessageId { get; set; }
}   