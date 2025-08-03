using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Api;
using FreelancePlatform.Dto.Services;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Api;

public class ServiceControllerTests
{
    private readonly AppDbContext _context;
    private readonly ServiceController _controller;
    
    public ServiceControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _controller = new ServiceController(_context);
    }

    [Fact]
    public async Task CreateService_ReturnsCreatedAtAction_WhenValid()
    {
        var dto = new CreateServiceDto
        {
            Title = "New Service",
            Description = "Desc",
            Price = 100
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
        
        var result = await _controller.CreateService(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var service = Assert.IsType<Service>(createdResult.Value);
        Assert.Equal(dto.Title, service.Title);
        Assert.Equal(userId, service.FreelancerId);
    }

    [Fact]
    public async Task UpdateService_ReturnsForbid_WhenUserNotOwner()
    {
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        
        var service = new Service
        {
            Id = 1,
            Title = "Old",
            Description = "My service",
            Price = 100,
            FreelancerId = "freelancer1"
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var dto = new UpdateServiceDto
        {
            Title = "New",
            Description = "New desc",
            Price = 200
        };
        
        var userId = "otherFreelancer";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
        
        var result = await _controller.UpdateService(1, dto);
        
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task UpdateService_ReturnsOk_WhenUserIsOwner()
    {
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        
        var service = new Service
        {
            Id = 1,
            Title = "Old",
            Description = "My service",
            Price = 100,
            FreelancerId = "freelancer1"
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var dto = new UpdateServiceDto
        {
            Title = "New",
            Description = "New desc",
            Price = 200
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
        
        var result = await _controller.UpdateService(1, dto);
        
        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
        
        Assert.Equal(dto.Title, result.Value.Title);
        Assert.Equal(dto.Description, result.Value.Description);
        Assert.Equal(dto.Price, result.Value.Price);
    }

    [Fact]
    public async Task GetServiceById_ReturnsService_WhenExists()
    {
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        
        var service = new Service
        {
            Id = 1,
            Title = "Old",
            Description = "My service",
            Price = 100,
            FreelancerId = "freelancer1"
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var result = await _controller.GetService(1);
        
        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.Id);
    }

    [Fact]
    public async Task DeleteService_ReturnsForbid_WhenUserNotOwner()
    {
        var service = new Service
        {
            Id = 1,
            Title = "Old",
            Description = "My service",
            Price = 100,
            FreelancerId = "freelancer1"
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var userId = "otherFreelancer";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
        
        var result = await _controller.DeleteService(1);
        
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteService_ReturnsNoContent_WhenUserIsOwner()
    {
        var service = new Service
        {
            Id = 2,
            Title = "Old",
            Description = "My service",
            Price = 100,
            FreelancerId = "freelancer2"
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var userId = "freelancer2";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
        
        var result = await _controller.DeleteService(2);
        
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetSerivces_ReturnsFilteredByStatus()
    {
        var freelancer1 = new IdentityUser { Id = "f1", UserName = "f1@test.com" };
        var freelancer2 = new IdentityUser { Id = "f2", UserName = "f2@test.com" };
        await _context.Users.AddRangeAsync(freelancer1, freelancer2);
        
        _context.Services.AddRange(
            new Service { Id = 1, Title = "Open", Description = "My service", Price = 100, Status = ServiceStatus.Available, FreelancerId = "f1" },
            new Service { Id = 2, Title = "Open", Description = "My service", Price = 100, Status = ServiceStatus.Unavailable, FreelancerId = "f2" }
        );
        await _context.SaveChangesAsync();
        
        var result = await _controller.ListServices(null, status: ServiceStatus.Available.ToString(), null, null, null);
        var services = Assert.IsAssignableFrom<IEnumerable<Service>>(result.Value);
        Assert.Single(services);
        Assert.Equal(ServiceStatus.Available, services.First().Status);
    }
    
    public void Dispose()
    {
        _context.Dispose();
    }
}