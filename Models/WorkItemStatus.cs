using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Models;

public enum WorkItemStatus
{
    [Display(Name = "Не начата")]
    NotStarted,
    [Display(Name = "В процессе")]
    InProgress,
    [Display(Name = "Выполнена")]
    Completed
}