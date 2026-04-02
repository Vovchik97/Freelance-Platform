using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Web;
using FreelancePlatform.Dto.Categories;
using FreelancePlatform.Dto.Projects;
using FreelancePlatform.Dto.Services;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
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
    private readonly CategorySuggestionService _categorySuggestionService;
    private readonly ServiceController _controller;

    public ServiceControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _categorySuggestionService = new CategorySuggestionService(_context);
        
        _mockUserManager = GetMockUserManager();
        _controller = new ServiceController(_context, _mockUserManager.Object, _categorySuggestionService);
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

        var result = await _controller.Index("One", ServiceStatus.Available.ToString(), 50, 300, null!, null);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Service>>(view.Model);
        Assert.Single(model);
        Assert.Equal("One", model.First().Title);
    }
    
    [Fact]
    public async Task Index_WithCategoryFilter_ReturnsFilteredServices()
    {
        var freelancer = new IdentityUser { Id = "f1", UserName = "f1@test.com" };
        await _context.Users.AddAsync(freelancer);

        var cat1 = new Category { Id = 1, Name = "Дизайн", IsActive = true, CreatedAt = DateTime.UtcNow };
        var cat2 = new Category { Id = 2, Name = "SEO", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.AddRange(cat1, cat2);

        _context.Services.AddRange(
            new Service { Title = "Design service", Description = "d", FreelancerId = "f1", Status = ServiceStatus.Available, Price = 100, CreatedAt = DateTime.UtcNow, Categories = new List<Category> { cat1 } },
            new Service { Title = "SEO service", Description = "d", FreelancerId = "f1", Status = ServiceStatus.Available, Price = 200, CreatedAt = DateTime.UtcNow, Categories = new List<Category> { cat2 } }
        );
        await _context.SaveChangesAsync();

        var result = await _controller.Index(null, null, null, null, null!, new List<int> { 1 });
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Service>>(view.Model);
        Assert.Single(model);
        Assert.Equal("Design service", model.First().Title);
    }

    [Fact]
    public async Task Details_ReturnsView_WhenServiceExists()
    {
        
        var service = new Service { Id = 1, Title = "s1", Description = "s", FreelancerId = "f1" };
        var freelancer = new IdentityUser { Id = "f1" };
        
        _context.Users.Add(freelancer);
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var result = await _controller.Details(1, null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<Service>(view.Model);
    }
    
    [Fact]
    public async Task Details_ReturnsNotFound_WhenServiceMissing()
    {
        var result = await _controller.Details(999, null);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_Get_ReturnsView()
    {
        var result = await _controller.Create();
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
    public async Task Create_Post_AutoAssignsCategories_WhenNoneSelected()
    {
        SetUser("freelancer1");

        var freelancer = new IdentityUser { Id = "freelancer1", Email = "f@test.com" };
        _context.Users.Add(freelancer);

        var cat = new Category { Id = 1, Name = "Дизайн", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(cat);
        await _context.SaveChangesAsync();

        var dto = new CreateServiceDto
        {
            Title = "Создание логотипа",
            Description = "Дизайн в Figma",
            Price = 300,
            Status = ServiceStatus.Available,
            CategoryIds = new List<int>()
        };

        var result = await _controller.Create(dto);

        Assert.IsType<RedirectToActionResult>(result);

        var created = await _context.Services
            .Include(s => s.Categories)
            .FirstAsync(s => s.Title == "Создание логотипа");
        Assert.NotEmpty(created.Categories);
        Assert.Contains(created.Categories, c => c.Name == "Дизайн");
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
    public async Task Edit_Post_AutoAssignsCategories_WhenAllRemoved()
    {
        SetUser("freelancer1");
        var cat = new Category { Id = 1, Name = "Другое", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(cat);

        var service = new Service
        {
            Id = 1,
            Title = "Какая-то услуга",
            Description = "Без ключевых слов",
            FreelancerId = "freelancer1",
            Categories = new List<Category> { cat }
        };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        var dto = new UpdateServiceDto
        {
            Title = "Какая-то услга",
            Description = "Без ключевых слов",
            CategoryIds = new List<int>()
        };
        
        var result = await _controller.Edit(1, dto);

        Assert.IsType<RedirectToActionResult>(result);

        var updated = await _context.Services
            .Include(p => p.Categories)
            .FirstAsync(p => p.Id == 1);
        Assert.NotEmpty(updated.Categories);
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

    [Fact]
    public async Task AddReview_RatingOutOfRange_RedirectWithError()
    {
        SetUser("client1");

        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );
        
        var result = await _controller.AddReview(1, "Bad rating", 6);
        
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal("Оценка должна быть от 1 до 5.", _controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task AddReview_NoCompletedOrder_RedirectWithError()
    {
        SetUser("client1");

        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );
        
        var service = new Service { Id = 1, Title = "s1", Description = "s1", Status = ServiceStatus.Available, FreelancerId = "freelancer1" };
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        var result = await _controller.AddReview(1, "Test rating", 4);
        
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal("Оставить отзыв можно только после выполнения заказа.", _controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task AddReview_ExistingReview_UpdateReview()
    {
        SetUser("client1");

        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );
        
        var service = new Service { Id = 1, Title = "s1", Description = "s1", Status = ServiceStatus.Available, FreelancerId = "freelancer1" };
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1", Status = OrderStatus.Completed };
        var review = new Review { Id = 1, ServiceId = 1, UserId = "client1", Rating = 4, Comment = "Test rating" };
        _context.Services.Add(service);
        _context.Orders.Add(order);
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();
        
        var result = await _controller.AddReview(1, "New rating", 5);
        
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        var updatedReview = await _context.Reviews.FirstAsync();
        Assert.Equal(5, updatedReview.Rating);
        Assert.Equal("New rating", updatedReview.Comment);
    }

    [Fact]
    public async Task AddReview_NewReview_CreateReview()
    {
        SetUser("client1");

        _controller.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>()
        );
        
        var service = new Service { Id = 1, Title = "s1", Description = "s1", Status = ServiceStatus.Available, FreelancerId = "freelancer1" };
        var order = new Order { Id = 1, ServiceId = 1, ClientId = "client1", Status = OrderStatus.Completed };
        _context.Services.Add(service);
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        var result = await _controller.AddReview(1, "Test rating", 5);
        
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        var review = await _context.Reviews.FirstOrDefaultAsync();
        Assert.NotNull(review);
        Assert.Equal("client1", review!.UserId);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Test rating", review.Comment);
    }
    
    [Fact]
    public async Task SuggestCategories_ReturnsSuggestedIds()
    {
        SetUser("freelancer1");

        var cat = new Category { Id = 1, Name = "Дизайн", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(cat);
        await _context.SaveChangesAsync();

        var request = new SuggestCategoriesRequest { Title = "Логотип", Description = "дизайн в figma" };
        var result = await _controller.SuggestCategories(request);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var ids = Assert.IsAssignableFrom<List<int>>(jsonResult.Value);
        Assert.Contains(1, ids);
    }

    [Fact]
    public async Task SuggestCategories_ReturnsEmpty_WhenRequestNull()
    {
        SetUser("freelancer1");

        var result = await _controller.SuggestCategories(null!);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var ids = Assert.IsAssignableFrom<List<int>>(jsonResult.Value);
        Assert.Empty(ids);
    }
}