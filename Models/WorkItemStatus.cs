using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Models;

/// <summary>
/// Определяет текущее состояние этапа выполнения работы.
/// </summary>
public enum WorkItemStatus
{
    [Display(Name = "Не начата")]
    NotStarted,
    [Display(Name = "В процессе")]
    InProgress,
    [Display(Name = "Выполнена")]
    Completed
}