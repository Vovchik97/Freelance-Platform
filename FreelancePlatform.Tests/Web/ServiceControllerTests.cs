using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Web;
using FreelancePlatform.Dto.Projects;
using FreelancePlatform.Dto.Services;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Web;

public class ServiceControllerTests
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly ServiceController _controller;

    public ServiceControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        
        _mockUserManager = GetMockUserManager();
        _controller = new ServiceController(_context, _mockUserManager.Object);
    }
    
    private static Mock<UserManager<IdentityUser>> GetMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        var mock = new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        mock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((ClaimsPrincipal principal) =>
            {
                return principal.FindFirstValue(ClaimTypes.NameIdentifier);
            });

        return mock;
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
    public async Task Index_WithFilters_ReturnsFilteredServices()
    {
        var freelancer1 = new IdentityUser { Id = "f1", UserName = "f1@test.com" };
        var freelancer2 = new IdentityUser { Id = "f2", UserName = "f2@test.com" };
        await _context.Users.AddRangeAsync(freelancer1, freelancer2);
        
        _context.Services.AddRange(
            new Service { Title = "One", Description = "One", FreelancerId = "f1", Status = ServiceStatus.Available, Price = 100, CreatedAt = DateTime.UtcNow },
            new Service { Title = "Two", Description = "Two", FreelancerId = "f2", Status = ServiceStatus.Available, Price = 200, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var result = await _controller.Index("One", ServiceStatus.Available.ToString(), 50, 300, null!);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Service>>(view.Model);
        Assert.Single(model);
        Assert.Equal("One", model.First().Title);
    }

    [Fact]
    public async Task Details_ReturnsView_WhenServiceExists()
    {
        
        var service = new Service { Id = 1, Title = "s1", Description = "s", FreelancerId = "f1" };
        var freelancer = new IdentityUser { Id = "f1" };
        
        _context.Users.Add(freelancer);
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var result = await _controller.Details(1);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<Service>(view.Model);
    }
    
    [Fact]
    public async Task Details_ReturnsNotFound_WhenServiceMissing()
    {
        var result = await _controller.Details(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void Create_Get_ReturnsView()
    {
        var result = _controller.Create();
        var view = Assert.IsType<ViewResult>(result);
    }
    
    [Fact]
    public async Task Create_Post_ReturnsRedirect_WhenValid()
    {
        SetUser("freelancer1");

        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "freelancer1" };
        var freelancer = new IdentityUser { Id = "freelancer1", Email = "freelancer1@test.com" };

        _context.Services.Add(service);
        _context.Users.Add(freelancer);
        await _context.SaveChangesAsync();

        var dto = new CreateServiceDto { Title = "p1", Description = "p1", Price = 500, Status = ServiceStatus.Available };
        var result = await _controller.Create(dto);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ServiceController.MyServices), redirect.ActionName);
    }
    
    [Fact]
    public async Task Edit_Get_ReturnsView_WhenValid()
    {
        SetUser("freelancer1");
        
        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "freelancer1" };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var result = await _controller.Edit(1);
        var view = Assert.IsType<ViewResult>(result);
    }
    
    [Fact]
    public async Task Edit_Post_UpdatesService_WhenValid()
    {
        SetUser("freelancer1");
        
        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "freelancer1" };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var dto = new UpdateServiceDto { Title = "updated" };
        var result = await _controller.Edit(1, dto);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ServiceController.MyServices), redirect.ActionName);

        var updated = await _context.Services.FindAsync(1);
        Assert.Equal("updated", updated!.Title);
    }
    
    [Fact]
    public async Task Edit_Post_UpdatesService_WhenInvalid()
    {
        SetUser("freelancer1");
        
        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "freelancer1" };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var dto = new UpdateServiceDto { Title = "updated" };
        var result = await _controller.Edit(999, dto);
        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task Delete_RemovesService_WhenValid()
    {
        SetUser("freelancer1");
        
        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "freelancer1" };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var result = await _controller.Delete(1);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ServiceController.MyServices), redirect.ActionName);
        
        var deleted = await _context.Services.FindAsync(1);
        Assert.Null(deleted);
    }
    
    [Fact]
    public async Task MyServices_ReturnsUserServices()
    {
        SetUser("freelancer1");
        
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };
        await _context.Users.AddAsync(freelancer);
        
        var service1 = new Service { Id = 1, Title = "s1", Description = "My description", FreelancerId = freelancer.Id, CreatedAt = DateTime.UtcNow };
        var service2 = new Service { Id = 2, Title = "s2", Description = "My description", FreelancerId = freelancer.Id, CreatedAt = DateTime.UtcNow };
        _context.Services.AddRange(service1, service2);
        await _context.SaveChangesAsync();

        var result = await _controller.MyServices();
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Service>>(view.Model);
        Assert.Equal(2, model.Count());

    }

    [Fact]
    public async Task AcceptOrder_SetsOrderAccepted_WhenValid()
    {
        SetUser("freelancer1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1", Status = OrderStatus.Pending };
        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "freelancer1" };
        _context.Services.Add(service);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var result = await _controller.AcceptOrder(1, 1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(OrderStatus.Accepted, _context.Orders.Find(1)!.Status);
    }
    
    [Fact]
    public async Task AcceptOrder_SetsOrderAccepted_NotFound()
    {
        SetUser("freelancer1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1", Status = OrderStatus.Pending };
        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "freelancer1" };
        _context.Services.Add(service);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var result = await _controller.AcceptOrder(5, 5);

        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task AcceptOrder_SetsOrderAccepted_Forbid()
    {
        SetUser("freelancer1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1", Status = OrderStatus.Pending };
        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "freelancer2" };
        _context.Services.Add(service);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var result = await _controller.AcceptOrder(1, 1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task RejectedOrder_SetsOrderRejected()
    {
        SetUser("freelancer1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1", Status = OrderStatus.Pending };
        var service = new Service { Id = 1, Title = "s1", Description = "s1", FreelancerId = "freelancer1" };
        _context.Services.Add(service);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var result = await _controller.RejectOrder(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(OrderStatus.Rejected, _context.Orders.Find(1)!.Status);
    }

    [Fact]
    public async Task CancelService_UpdatesStatus()
    {
        SetUser("freelancer1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var service = new Service { Id = 1, Title = "s1", Description = "s1", Status = ServiceStatus.Unavailable, FreelancerId = "freelancer1" };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var result = await _controller.CancelService(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ServiceStatus.Unavailable, _context.Services.Find(1)!.Status);
    }

    [Fact]
    public async Task ResumeService_UpdatesStatus_WhenValid()
    {
        SetUser("freelancer1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };  
        var service = new Service { Id = 1, Title = "s1", Description = "s1", Status = ServiceStatus.Unavailable, FreelancerId = "freelancer1" };
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1", Status = OrderStatus.Accepted };

        _context.Users.Add(freelancer);
        _context.Services.Add(service);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var result = await _controller.ResumeService(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ServiceStatus.Available, _context.Services.Find(1)!.Status);
    }
    
    [Fact]
    public async Task ResumeService_UpdatesStatus_NotFound()
    {
        SetUser("freelancer1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };  
        var service = new Service { Id = 1, Title = "s1", Description = "s1", Status = ServiceStatus.Unavailable, FreelancerId = "freelancer1" };
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1", Status = OrderStatus.Accepted };

        _context.Users.Add(freelancer);
        _context.Services.Add(service);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var result = await _controller.ResumeService(999);

        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task ResumeService_UpdatesStatus_BadRequest()
    {
        SetUser("freelancer1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var freelancer = new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" };  
        var service = new Service { Id = 1, Title = "s1", Description = "s1", Status = ServiceStatus.Available, FreelancerId = "freelancer1" };
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1", Status = OrderStatus.Accepted };

        _context.Users.Add(freelancer);
        _context.Services.Add(service);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var result = await _controller.ResumeService(1);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}