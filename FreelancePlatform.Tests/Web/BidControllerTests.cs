using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Web;
using FreelancePlatform.Dto.Bids;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Web;

public class BidControllerTests
{
    private readonly AppDbContext _context;
    private readonly BidController _controller;

    public BidControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        var emailSender = new FakeEmailSender();
        _controller = new BidController(_context, emailSender);
    }

    private void SetUser(string userId, string role = "Freelancer")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task Details_ReturnsView_WhenBidExists()
    {
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "f1" };
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "c1" };
        var client = new IdentityUser { Id = "c1" };
        var freelancer = new IdentityUser { Id = "f1" };
        
        _context.Users.AddRange(client, freelancer);
        _context.Projects.Add(project);
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();

        var result = await _controller.Details(1);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<Bid>(view.Model);
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenBidMissing()
    {
        var result = await _controller.Details(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        var result = _controller.Create(5);
        var view = Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Create_Post_ReturnsRedirect_WhenValid()
    {
        SetUser("freelancer1");

        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client1" };
        var client = new IdentityUser { Id = "client1", Email = "client1@test.com" };
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };

        _context.Projects.Add(project);
        _context.Users.AddRange(client, freelancer);
        await _context.SaveChangesAsync();

        var dto = new CreateBidDto { ProjectId = 1, Amount = 100, Comment = "test", DurationInDays = 3 };
        var resullt = await _controller.Create(dto);

        var redirect = Assert.IsType<RedirectToActionResult>(resullt);
        Assert.Equal(nameof(BidController.MyBids), redirect.ActionName);
    }

    [Fact]
    public async Task Create_Post_ReturnsView_WhenAlreadyBid()
    {
        SetUser("freelancer1");
        
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1" };
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client1" };

        _context.Projects.Add(project);
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();

        var dto = new CreateBidDto { ProjectId = 1, Amount = 100, Comment = "test", DurationInDays = 3 };
        var result = await _controller.Create(dto);

        var view = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
    }

    [Fact]
    public async Task Edit_Get_ReturnsView_WhenValid()
    {
        SetUser("freelancer1");
        
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", Amount = 200 };
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();

        var result = await _controller.Edit(1);
        var view = Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Edit_Post_UpdatesBid_WhenValid()
    {
        SetUser("freelancer1");
        
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", Amount = 200 };
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        
        var dto = new UpdateBidDto { Amount = 300, Comment = "updated", DurationInDays = 10 };
        var result = await _controller.Edit(1, dto);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(BidController.MyBids), redirect.ActionName);

        var updated = await _context.Bids.FindAsync(1);
        Assert.Equal(300, updated!.Amount);
    }

    [Fact]
    public async Task Delete_RemovesBid_WhenValid()
    {
        SetUser("freelancer1");
        
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1" };
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        
        var result = await _controller.Delete(1);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(BidController.MyBids), redirect.ActionName);
        
        var deleted = await _context.Bids.FindAsync(1);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task MyBids_ReturnsUserBids()
    {
        SetUser("freelancer1");
        
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        var freelancer1 = new IdentityUser { Id = "f1", UserName = "f1@test.com" };
        var freelancer2 = new IdentityUser { Id = "f2", UserName = "f2@test.com" };
        await _context.Users.AddRangeAsync(client, freelancer1, freelancer2);
        
        var project1 = new Project { Id = 1, Title = "Project1", Description = "My description", ClientId = client.Id, CreatedAt = DateTime.UtcNow };
        var project2 = new Project { Id = 2, Title = "Project2", Description = "My description", ClientId = client.Id, CreatedAt = DateTime.UtcNow };
        _context.Projects.AddRange(project1, project2);
        
        _context.Bids.Add(new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", CreatedAt = DateTime.UtcNow });
        _context.Bids.Add(new Bid { Id = 2, ProjectId = 2, FreelancerId = "freelancer1", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var result = await _controller.MyBids();
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Bid>>(view.Model);
        Assert.Equal(2, model.Count());

    }
}