using System.ComponentModel.DataAnnotations;
using FreelancePlatform.Models;

namespace FreelancePlatform.Dto.Projects;

public class UpdateProjectDto
{
    [Required(ErrorMessage = "Поле «Название» обязательно.")]
    public string Title { get; set; } = default!;
    [Required(ErrorMessage = "Поле «Описание» обязательно.")]
    public string Description { get; set; } = default!;
    [Required(ErrorMessage = "Поле «Бюджет» обязательно.")]
    [Range(1, int.MaxValue, ErrorMessage = "Укажите корректный бюджет.")]
    public decimal Budget { get; set; }
    public ProjectStatus Status { get; set; }
}