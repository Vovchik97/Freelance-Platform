using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Api;
using FreelancePlatform.Dto.Orders;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Api;

public class OrderControllerTests
{
    private readonly AppDbContext _context;
    private readonly OrderController _controller;

    public OrderControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _controller = new OrderController(_context);
    }

    [Fact]
    public async Task GetOrderById_ReturnsOrder_WhenFound()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        
        var service = new Service { Title = "Test Service", Description = "Desc", Price = 1000, FreelancerId = freelancer.Id, CreatedAt = DateTime.UtcNow };
        _context.Services.Add(service);
        
        var order = new Order { Id = 42, ServiceId = 1, ClientId = "client1" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var result = await _controller.GetOrder(42);
        
        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
        Assert.Equal(42, result.Value.Id);
    }

    [Fact]
    public async Task GetOrderById_ReturnsBadRequest_WhenNotFound()
    {
        var result = await _controller.GetOrder(999);
        Assert.IsType<BadRequestResult>(result.Result);
    }

    [Fact]
    public async Task ListOrders_ReturnsFilteredByServiceId()
    {
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        var client1 = new IdentityUser { Id = "c1", UserName = "c1@test.com" };
        var client2 = new IdentityUser { Id = "c2", UserName = "c2@test.com" };
        await _context.Users.AddRangeAsync(freelancer, client1, client2);
        
        var service1 = new Service { Id = 1, Title = "Service1", Description = "My description", FreelancerId = freelancer.Id, CreatedAt = DateTime.UtcNow };
        var service2 = new Service { Id = 2, Title = "Service2", Description = "My description", FreelancerId = freelancer.Id, CreatedAt = DateTime.UtcNow };
        _context.Services.AddRange(service1, service2);
        
        _context.Orders.Add(new Order { Id = 1, ServiceId = 1, ClientId = "c1" });
        _context.Orders.Add(new Order { Id = 2, ServiceId = 2, ClientId = "c2" });
        await _context.SaveChangesAsync();
        
        var result = await _controller.ListOrders(serviceId: 1, clientId: null);
        var orders = Assert.IsAssignableFrom<IEnumerable<Order>>(result.Value);
        Assert.Single(orders);
        Assert.Equal(1, orders.First().ServiceId);
    }

    [Fact]
    public async Task CreateOrder_ReturnsCreatedAtAction_WhenValid()
    {
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        var service = new Service { Id = 1, Title = "Test", Description = "My description", Price = 100, FreelancerId = "freelancer1", CreatedAt = DateTime.UtcNow };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var dto = new CreateOrderDto
        {
            ServiceId = 1,
            Comment = "My comment",
            DurationInDays = 10
        };

        var userId = "client1";

        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
        
        var result = await _controller.CreateOrder(dto);
        
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var order = Assert.IsType<Order>(createdResult.Value);
        Assert.Equal(dto.ServiceId, order.ServiceId);
        Assert.Equal(userId, order.ClientId);
    }
    
    [Fact]
    public async Task CreateOrder_ReturnsConflict_WhenDuplicateOrder()
    {
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        var service = new Service { Id = 1, Title = "Test", Description = "My description", Price = 1000, FreelancerId = "freelancer1", CreatedAt = DateTime.UtcNow };
        _context.Services.Add(service);
        _context.Orders.Add(new Order { ServiceId = 1, ClientId = "client1", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();
        
        var dto = new CreateOrderDto
        {
            ServiceId = 1,
            Comment = "Another order",
            DurationInDays = 5
        };
        
        var userId = "client1";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, userId) }));
        _controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };
        
        var resullt = await _controller.CreateOrder(dto);
        
        var conflictResult = Assert.IsType<ConflictObjectResult>(resullt.Result);
        Assert.Equal("You have already applied for this project.", conflictResult.Value);
    }
    
    [Fact]
    public async Task UpdateOrder_ReturnsOk_WhenValid()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        
        var service = new Service { Title = "Test Service", Description = "Desc", Price = 1000, FreelancerId = freelancer.Id, CreatedAt = DateTime.UtcNow };
        _context.Services.Add(service);
        
        var order = new Order { Id = 3, ServiceId = 1, ClientId = "client1" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, "client1")
        }));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var dto = new UpdateOrderDto { Comment = "Updated", DurationInDays = 7 };

        var result = await _controller.UpdateOrder(3, dto);

        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
        Assert.Equal(7, result.Value.DurationInDays);
    }

    [Fact]
    public async Task UpdateOrder_ReturnsForbid_WhenDifferentClient()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        
        var service = new Service { Title = "Test Service", Description = "Desc", Price = 1000, FreelancerId = freelancer.Id, CreatedAt = DateTime.UtcNow };
        _context.Services.Add(service);
        
        var order = new Order { Id = 3, ServiceId = 1, ClientId = "client1" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "not-owner")
        }));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var dto = new UpdateOrderDto { Comment = "Updated", DurationInDays = 7 };
        var result = await _controller.UpdateOrder(3, dto);
        Assert.IsType<ForbidResult>(result.Result);
    }
    
    [Fact]
    public async Task UpdateOrder_ReturnsNotFound_WhenNoOrder()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "client1")
        }));
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var dto = new UpdateOrderDto { Comment = "Updated", DurationInDays = 7 };
        var result = await _controller.UpdateOrder(999, dto);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteOrder_ReturnsNoContent_WhenSuccess()
    {
        var order = new Order { Id = 3, ServiceId = 1, ClientId = "client1" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "client1") }));
        _controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };
        
        var result = await _controller.DeleteOrder(3);
        
        Assert.IsType<NoContentResult>(result.Result);
        Assert.False(_context.Orders.Any(o => o.Id == 3));
    }

    [Fact]
    public async Task DeleteOrder_ReturnsForbid_WhenDifferentUser()
    {
        var order = new Order { Id = 3, ServiceId = 1, ClientId = "client1" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new Claim(ClaimTypes.NameIdentifier, "not-owner") }));
        _controller.ControllerContext = new ControllerContext() { HttpContext = new DefaultHttpContext() { User = user } };
        
        var result = await _controller.DeleteOrder(3);
        
        Assert.IsType<ForbidResult>(result.Result);
    }

    private void Dispose()
    {
        _context.Dispose();
    }
}