using System.ComponentModel.DataAnnotations;
namespace FreelancePlatform.Dto.Auth;

public class RegisterDto
{
    [Required(ErrorMessage = "Введите Email.")]
    public required string Email { get; set; }
    [Required(ErrorMessage = "Введите пароль.")]
    public required string Password { get; set; }
    [Required(ErrorMessage = "Выберите роль")]
    public required string Role { get; set; }
}