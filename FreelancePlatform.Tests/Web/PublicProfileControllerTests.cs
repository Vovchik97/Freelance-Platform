using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Web;
using FreelancePlatform.Dto.Profiles;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Web;

public class PublicProfileControllerTests
{
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private Mock<UserManager<IdentityUser>> GetUserManagerMock()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private void SetUser(PublicProfileController controller, string userId, string userName = "testUser")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Public_ReturnsNotFound_WhenUserIdIsNull()
    {
        var db = GetDbContext();
        var userManager = GetUserManagerMock();
        var controller = new PublicProfileController(db, userManager.Object);
        
        var result = await controller.Public(null!);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Public_RetrunsNotFound_WhenUserNotFound()
    {
        var db = GetDbContext();
        var userManager = GetUserManagerMock();
        userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync((IdentityUser?)null);
        var controller = new PublicProfileController(db, userManager.Object);
        
        var result = await controller.Public("userId");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Public_ReturnsView_WithDto_WhenUserExists()
    {
        var db = GetDbContext();
        var userManager = GetUserManagerMock();
        
        var user = new IdentityUser { Id = "u1", UserName = "testUser" };
        userManager.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        
        db.UserProfiles.Add(new UserProfile { UserId = "u1", AboutMe = "testAboutMe" });
        await db.SaveChangesAsync();

        var controller = new PublicProfileController(db, userManager.Object);
        
        var result = await controller.Public("u1");

        var view = Assert.IsType<ViewResult>(result);
        var dto = Assert.IsType<PublicProfileDto>(view.Model);
        Assert.Equal("testUser", dto.UserName);
        Assert.Equal("testAboutMe", dto.AboutMe);
    }

    [Fact]
    public async Task My_CreatesProfile_WhenNotExists()
    {
        var db = GetDbContext();
        var userManager = GetUserManagerMock();
        var user = new IdentityUser { Id = "u1", UserName = "testUser" };
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var controller = new PublicProfileController(db, userManager.Object);
        SetUser(controller, "u1");

        var result = await controller.My();
        
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Public", redirect.ActionName);
        
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == "u1");
        Assert.NotNull(profile);
    }

    [Fact]
    public async Task EditGet_ReturnsUnauthorized_WhenUserNull()
    {
        var db = GetDbContext();
        var userManager = GetUserManagerMock();
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((IdentityUser?)null);
        
        var controller = new PublicProfileController(db, userManager.Object);
        
        var result = await controller.Edit();
        
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task EditGet_ReturnsView_WhenExists()
    {
        var db = GetDbContext();
        var userManager = GetUserManagerMock();
        var user = new IdentityUser { Id = "u1", UserName = "testUser" };
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        
        db.UserProfiles.Add(new UserProfile { UserId = "u1", AboutMe = "testAboutMe" });
        await db.SaveChangesAsync();

        var controller = new PublicProfileController(db, userManager.Object);
        SetUser(controller, "u1");
        
        var result = await controller.Edit();
        
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UserProfile>(view.Model);
        Assert.Equal("testAboutMe", model.AboutMe);
    }

    [Fact]
    public async Task EditPost_CreatesProfile_WhenNotExists()
    {
        var db = GetDbContext();
        var userManager = GetUserManagerMock();
        var user = new IdentityUser { Id = "u1", UserName = "testUser" };
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        
        var controller = new PublicProfileController(db, userManager.Object);
        SetUser(controller, "u1");
        
        var model = new UserProfile { UserId = "u1", AboutMe = "testAboutMe" };
        var result = await controller.Edit(model);
        
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Public", redirect.ActionName);
        
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == "u1");
        Assert.Equal("testAboutMe", profile?.AboutMe);
    }

    [Fact]
    public async Task EditPost_UpdatesProfile_WhenExists()
    {
        var db = GetDbContext();
        var userManager = GetUserManagerMock();
        var user = new IdentityUser { Id = "u1", UserName = "testUser" };
        userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        
        db.UserProfiles.Add(new UserProfile { UserId = "u1", AboutMe = "testAboutMe" });
        await db.SaveChangesAsync();

        var controller = new PublicProfileController(db, userManager.Object);
        SetUser(controller, "u1");
        
        var model = new UserProfile { UserId = "u1", AboutMe = "testAboutMe2" };
        var result = await controller.Edit(model);
        
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Public", redirect.ActionName);
        
        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == "u1");
        Assert.Equal("testAboutMe2", profile?.AboutMe);
    }
}