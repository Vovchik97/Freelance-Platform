using System.ComponentModel.DataAnnotations;
namespace FreelancePlatform.Dto.Auth;

public class RegisterDto
{
    [Required(ErrorMessage = "Введите Email.")]
    [EmailAddress(ErrorMessage = "Введите корректный Email.")]
    public required string Email { get; set; }
    [Required(ErrorMessage = "Введите пароль.")]
    [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов.")]
    public required string Password { get; set; }
    [Required(ErrorMessage = "Выберите роль")]
    public required string Role { get; set; }
}