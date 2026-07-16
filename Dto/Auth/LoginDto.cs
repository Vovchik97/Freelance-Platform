using System.ComponentModel.DataAnnotations;

namespace FreelancePlatform.Dto.Auth;

public class LoginDto
{
    [Required(ErrorMessage = "Введите Email.")]
    [EmailAddress(ErrorMessage = "Некорректный Email.")]
    public required string Email { get; set; }
    [Required(ErrorMessage = "Введите пароль.")]
    public required string Password { get; set; }
}