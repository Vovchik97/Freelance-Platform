using FreelancePlatform.Controllers.Api;
using FreelancePlatform.Dto.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Api;

public class AuthControllerTests
{
    private readonly Mock<UserManager<IdentityUser>> _userManagerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        _userManagerMock = new Mock<UserManager<IdentityUser>>(store.Object, null, null, null, null, null, null, null, null);
        
        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c["Jwt:Key"]).Returns("ThisIsAVeryVeryStrongJwtKeyThatIsLongEnough123!");
        _configMock.Setup(c => c["Jwt:Issuer"]).Returns("issuer");
        _configMock.Setup(c => c["Jwt:Audience"]).Returns("audience");

        _controller = new AuthController(_userManagerMock.Object, _configMock.Object);
    }

    [Fact]
    public async Task Register_ReturnsOk_WithToken_WhenSuccess()
    {
        var dto = new RegisterDto { Email = "test@test.com", Password = "Password123!", Role = "Client" };
        var user = new IdentityUser { UserName = dto.Email, Email = dto.Email };

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), dto.Password))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<IdentityUser>(), dto.Role))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.Register(dto);
        
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.Contains("token", okResult.Value.ToString());
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenCreateFails()
    {
        var dto = new RegisterDto { Email = "fail@test.com", Password = "bad", Role = "Client" };
        var user = new IdentityUser { UserName = dto.Email, Email = dto.Email };

        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), dto.Password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password to weak" }));
        
        var result = await _controller.Register(dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task Login_ReturnsOk_WithToken_WhenSuccess()
    {
        var dto = new LoginDto { Email = "test@test.com", Password = "Password123!" };
        var user = new IdentityUser { UserName = dto.Email, Email = dto.Email };

        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, dto.Password)).ReturnsAsync(true);
        _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Client" });

        var result = await _controller.Login(dto);
        
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        Assert.Contains("token", okResult.Value.ToString());
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenInvalid()
    {
        var dto = new LoginDto { Email = "test@test.com", Password = "wrongpass" };

        _userManagerMock.Setup(x => x.FindByEmailAsync(dto.Email));
        _userManagerMock.Setup(x => x.CheckPasswordAsync(It.IsAny<IdentityUser>(), dto.Password)).ReturnsAsync(false);
        
        var result = await _controller.Login(dto);
        
        Assert.IsType<UnauthorizedResult>(result);
    }
}