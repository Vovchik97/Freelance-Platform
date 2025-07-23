using System.ComponentModel.DataAnnotations;
namespace FreelancePlatform.Dto.Orders;

public class UpdateOrderDto
{
    public string? Comment { get; set; }
    [Required(ErrorMessage = "Поле «Количество дней» обязательно.")]
    [Range(1, int.MaxValue, ErrorMessage = "Укажите корректный срок.")]
    public int DurationInDays { get; set; }
}