namespace FreelancePlatform.Models;

/// <summary>
/// Определяет текущий статус обработки жалобы.
/// </summary>
public enum ReportStatus
{
    Pending = 0,
    Resolved = 1,
    Rejected = 2
}