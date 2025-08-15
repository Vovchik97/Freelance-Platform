namespace FreelancePlatform.Models;

public class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public Chat Chat { get; set; } = null!;

    public string SenderId { get; set; } = null!;
    public string Text { get; set; } = null!;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;
    
    public string? AttachmentUrl { get; set; } // Ссылка на файл
    public string? AttachmentName { get; set; } // Имя файла (для отображения)
    public string? AttachmentType { get; set; } // MIME-тип (image/png и т.п.)
    
    public int? ParentMessageId { get; set; }  // для связи вложения с основным сообщением
    public Message? ParentMessage { get; set; }
    public ICollection<Message> Attachments { get; set; } = new List<Message>();  // вложения
}