using System.ComponentModel.DataAnnotations;
namespace FreelancePlatform.Dto.Auth;

public class LoginDto
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}