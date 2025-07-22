using System.ComponentModel.DataAnnotations;
using FreelancePlatform.Models;

namespace FreelancePlatform.Dto.Services;

public class UpdateServiceDto
{
    [Required(ErrorMessage = "Поле «Название» обязательно.")]
    public string Title { get; set; } = default!;
    [Required(ErrorMessage = "Поле «Описание» обязательно.")]
    public string Description { get; set; } = default!;
    [Required(ErrorMessage = "Поле «Цена» обязательно.")]
    [Range(1, int.MaxValue, ErrorMessage = "Укажите корректную цену.")]
    public decimal Price { get; set; }
    public ServiceStatus Status { get; set; }
}