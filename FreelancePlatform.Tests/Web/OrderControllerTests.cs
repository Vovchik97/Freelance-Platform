using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Web;
using FreelancePlatform.Dto.Orders;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Web;

public class OrderControllerTests
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly OrderController _controller;

    public OrderControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        
        var emailSender = new FakeEmailSender();
        _mockUserManager = GetMockUserManager();
        _controller = new OrderController(_context, _mockUserManager.Object, emailSender);
    }
    
    private static Mock<UserManager<IdentityUser>> GetMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        var mock = new Mock<UserManager<IdentityUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        mock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ClaimsPrincipal principal) =>
            {
                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                return new IdentityUser
                {
                    Id = userId,
                    UserName = userId + "@test.com",
                    Email = userId + "@test.com"
                };
            });

        return mock;
    }
    
    private void SetUser(string userId, string role = "Client")
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
    public async Task Details_ReturnsView_WhenOrderExists()
    {
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "f1" };
        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "f1" };
        var client = new IdentityUser { Id = "c1" };
        var freelancer = new IdentityUser { Id = "f1" };
        
        _context.Users.AddRange(client, freelancer);
        _context.Services.Add(service);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _controller.Details(1);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<Order>(view.Model);
    }
    
    [Fact]
    public async Task Details_ReturnsNotFound_WhenOrderMissing()
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
        SetUser("client1");

        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "freelancer1" };
        var client = new IdentityUser { Id = "client1", Email = "client1@test.com" };
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };

        _context.Services.Add(service);
        _context.Users.AddRange(client, freelancer);
        await _context.SaveChangesAsync();

        var dto = new CreateOrderDto { ServiceId = 1, Comment = "test", DurationInDays = 3 };
        var result = await _controller.Create(dto);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(OrderController.MyOrders), redirect.ActionName);
    }
    
    [Fact]
    public async Task Create_Post_ReturnsView_WhenAlreadyOrder()
    {
        SetUser("client1");
        
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1" };
        var service = new Service { Id = 1, Title = "p1", Description = "p1", FreelancerId = "freelancer1" };

        await _context.Users.AddRangeAsync(freelancer, client);
        _context.Services.Add(service);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var dto = new CreateOrderDto { ServiceId = 1, Comment = "test", DurationInDays = 3 };
        var result = await _controller.Create(dto);

        var view = Assert.IsType<ViewResult>(result);
        Assert.False(_controller.ModelState.IsValid);
    }
    
    [Fact]
    public async Task Edit_Get_ReturnsView_WhenValid()
    {
        SetUser("client1");
        
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var result = await _controller.Edit(1);
        var view = Assert.IsType<ViewResult>(result);
    }
    
    [Fact]
    public async Task Edit_Post_UpdatesOrder_WhenValid()
    {
        SetUser("client1");
        
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var dto = new UpdateOrderDto { Comment = "updated", DurationInDays = 10 };
        var result = await _controller.Edit(1, dto);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(OrderController.MyOrders), redirect.ActionName);

        var updated = await _context.Orders.FindAsync(1);
        Assert.Equal(10, updated!.DurationInDays);
    }
    
    [Fact]
    public async Task Delete_RemovesOrder_WhenValid()
    {
        SetUser("client1");
        
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1" };
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var result = await _controller.Delete(1);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(OrderController.MyOrders), redirect.ActionName);
        
        var deleted = await _context.Orders.FindAsync(1);
        Assert.Null(deleted);
    }
    
    [Fact]
    public async Task MyOrders_ReturnsUserOrders()
    {
        SetUser("client1");
        
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        var client1 = new IdentityUser { Id = "c1", UserName = "c1@test.com" };
        var client2 = new IdentityUser { Id = "c2", UserName = "c2@test.com" };
        await _context.Users.AddRangeAsync(freelancer, client1, client2);
        
        var service1 = new Service { Id = 1, Title = "Service1", Description = "My description", FreelancerId = freelancer.Id, CreatedAt = DateTime.UtcNow };
        var service2 = new Service { Id = 2, Title = "Service2", Description = "My description", FreelancerId = freelancer.Id, CreatedAt = DateTime.UtcNow };
        _context.Services.AddRange(service1, service2);
        
        _context.Orders.Add(new Order { Id = 1, ServiceId = 1, ClientId = "client1", CreatedAt = DateTime.UtcNow });
        _context.Orders.Add(new Order { Id = 2, ServiceId = 2, ClientId = "client1", CreatedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var result = await _controller.MyOrders();
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Order>>(view.Model);
        Assert.Equal(2, model.Count());

    }
}