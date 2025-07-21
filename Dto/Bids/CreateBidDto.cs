using System.ComponentModel.DataAnnotations;
namespace FreelancePlatform.Dto.Bids;

public class CreateBidDto
{
    public int ProjectId { get; set; }
    [Required(ErrorMessage = "Поле «Сумма» обязательно.")]
    [Range(1, int.MaxValue, ErrorMessage = "Укажите корректную сумму.")]
    public decimal Amount { get; set; }
    public string? Comment { get; set; }
    [Required(ErrorMessage = "Поле «Количество дней» обязательно.")]
    [Range(1, 365, ErrorMessage = "Укажите корректный срок.")]
    public int DurationInDays { get; set; }
}