using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FreelancePlatform.Areas.Identity.Pages.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FreelancePlatform.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _config;


    public AuthController(UserManager<IdentityUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config = config;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var user = new IdentityUser { UserName = model.Email, Email = model.Email };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, model.Role);
            return Ok(new { token = GenerateJwtToken(user) });
        }

        return BadRequest(result.Errors);
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        {
            var roles = await _userManager.GetRolesAsync(user);
            return Ok(new { token = GenerateJwtToken(user, roles) });
        }

        return Unauthorized();
    }
    
    private string GenerateJwtToken(IdentityUser user, IList<string> roles = null)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email),
        };

        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Append(new Claim(ClaimTypes.Role, role));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class RegisterModel
{
    public string Email { get; set; }
    public string Password { get; set; }
    public string Role { get; set; }
}

public class LoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}