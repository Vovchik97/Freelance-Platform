namespace FreelancePlatform.Models;

/// <summary>
/// Представляет модель данных для отображения страницы ошибки.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}