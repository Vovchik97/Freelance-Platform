using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Api;
using FreelancePlatform.Dto.Bids;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Api;

public class BidControllerTests
{
    private readonly AppDbContext _context;
    private readonly BidController _controller;
    
    public BidControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        
        _controller = new BidController(_context);
    }

    [Fact]
    public async Task GetBidById_ReturnsBid_WhenFound()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        
        var project = new Project { Title = "Test Project", Description = "Desc", Budget = 1000, ClientId = client.Id, CreatedAt = DateTime.UtcNow };
        _context.Projects.Add(project);
        
        var bid = new Bid { Id = 42, ProjectId = 1, FreelancerId = "freelancer1", Amount = 500 };
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();

        var result = await _controller.GetBid(42);
        
        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
        Assert.Equal(42, result.Value.Id);
    }

    [Fact]
    public async Task GetBidById_ReturnsBadRequest_WhenNotFound()
    {
        var result = await _controller.GetBid(999);
        Assert.IsType<BadRequestResult>(result.Result);
    }
    
    [Fact]
    public async Task ListBids_ReturnsFilteredByProjectId()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        var freelancer1 = new IdentityUser { Id = "f1", UserName = "f1@test.com" };
        var freelancer2 = new IdentityUser { Id = "f2", UserName = "f2@test.com" };
        await _context.Users.AddRangeAsync(client, freelancer1, freelancer2);
        
        var project1 = new Project { Id = 1, Title = "Project1", Description = "My description", ClientId = client.Id, CreatedAt = DateTime.UtcNow };
        var project2 = new Project { Id = 2, Title = "Project2", Description = "My description", ClientId = client.Id, CreatedAt = DateTime.UtcNow };
        _context.Projects.AddRange(project1, project2);
        
        _context.Bids.Add(new Bid { Id = 1, ProjectId = 1, FreelancerId = "f1" });
        _context.Bids.Add(new Bid { Id = 2, ProjectId = 2, FreelancerId = "f2" });
        await _context.SaveChangesAsync();

        var result = await _controller.ListBids(projectId: 1, freelancerId: null);
        var bids = Assert.IsAssignableFrom<IEnumerable<Bid>>(result.Value);
        Assert.Single(bids);
        Assert.Equal(1, bids.First().ProjectId);
    }

    [Fact]
    public async Task CreateBid_ReturnsCreatedAtAction_WhenValid()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        var project = new Project { Id = 1, Title = "Test", Description = "My description", Budget = 1000, ClientId = "client1", CreatedAt = DateTime.UtcNow };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var dto = new CreateBidDto
        {
            ProjectId = 1,
            Amount = 500,
            Comment = "My bid",
            DurationInDays = 10
        };

        var userId = "freelancer1";

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
        
        var result = await _controller.CreateBid(dto);
        
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var bid = Assert.IsType<Bid>(createdResult.Value);
        Assert.Equal(dto.ProjectId, bid.ProjectId);
        Assert.Equal(userId, bid.FreelancerId);
    }

    [Fact]
    public async Task CreateBid_ReturnsConflict_WhenDuplicateBid()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        var project = new Project { Id = 1, Title = "Test", Description = "My description", Budget = 1000, ClientId = "client1", CreatedAt = DateTime.UtcNow };
        _context.Projects.Add(project);
        _context.Bids.Add(new Bid { ProjectId = 1, FreelancerId = "freelancer1", Amount = 200, CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        
        var dto = new CreateBidDto
        {
            ProjectId = 1,
            Amount = 300,
            Comment = "Another bid",
            DurationInDays = 5
        };
        
        var userId = "freelancer1";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
        _controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };
        
        var result = await _controller.CreateBid(dto);
        
        var conflictResult = Assert.IsType<ConflictObjectResult>(result.Result);
        Assert.Equal("You have already applied for this project.", conflictResult.Value);
    }
    
    [Fact]
    public async Task UpdateBid_ReturnsOk_WhenValid()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        
        var project = new Project { Id = 1, Title = "Test", Description = "My description", Budget = 1000, ClientId = "client1", CreatedAt = DateTime.UtcNow };
        _context.Projects.Add(project);
        
        var bid = new Bid { Id = 3, ProjectId = 1, FreelancerId = "freelancer1", Amount = 100 };
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "freelancer1")
        }));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var dto = new UpdateBidDto { Amount = 150, Comment = "Updated", DurationInDays = 5 };

        var result = await _controller.UpdateBid(3, dto);

        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
        Assert.Equal(150, result.Value.Amount);
    }

    [Fact]
    public async Task UpdateBid_ReturnsForbid_WhenDifferentFreelancer()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        
        var freelancer = new IdentityUser { Id = "owner", UserName = "owner@test.com" };
        await _context.Users.AddAsync(freelancer);
        
        var project = new Project { Id = 1, Title = "Test", Description = "My description", Budget = 1000, ClientId = "client1", CreatedAt = DateTime.UtcNow };
        _context.Projects.Add(project);
        
        var bid = new Bid { Id = 4, ProjectId = 1, FreelancerId = "owner", Amount = 100 };
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "not-owner")
        }));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var dto = new UpdateBidDto { Amount = 150 };

        var result = await _controller.UpdateBid(4, dto);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task UpdateBid_ReturnsNotFound_WhenNoBid()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "freelancer1")
        }));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var dto = new UpdateBidDto { Amount = 150 };
        var result = await _controller.UpdateBid(999, dto);

        Assert.IsType<NotFoundResult>(result.Result);
    }


    [Fact]
    public async Task DeleteBid_ReturnsNoContent_WhenSeccess()
    {
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", Amount = 100, CreatedAt = DateTime.UtcNow };
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "freelancer1") }));
        _controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };
        
        var result = await _controller.DeleteBid(1);
        
        Assert.IsType<NoContentResult>(result.Result);
        Assert.False(_context.Bids.Any(b => b.Id == 1));
    }
    
    [Fact]
    public async Task DeleteBid_ReturnsForbid_WhenDifferentUser()
    {
        var bid = new Bid { Id = 5, ProjectId = 1, FreelancerId = "owner", Amount = 100 };
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "not-owner")
        }));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var result = await _controller.DeleteBid(5);
        Assert.IsType<ForbidResult>(result.Result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}