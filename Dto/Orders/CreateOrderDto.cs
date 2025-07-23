using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Dto.Orders;

public class CreateOrderDto
{
    public int ServiceId { get; set; }
    public string? Comment { get; set; }
    [Required(ErrorMessage = "Поле «Количество дней» обязательно.")]
    [Range(1, 365, ErrorMessage = "Укажите корректный срок.")]
    public int DurationInDays { get; set; }
}