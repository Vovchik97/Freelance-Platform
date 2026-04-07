using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Web;
using FreelancePlatform.Dto.Categories;
using FreelancePlatform.Dto.Projects;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Web;

public class ProjectControllerTests
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly Mock<BalanceService> _balanceService;
    private readonly CategorySuggestionService _categorySuggestionService;
    private readonly RecommendationService _recommendationService;
    private readonly ProjectController _controller;

    public ProjectControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _balanceService = new Mock<BalanceService>(_context);
        _categorySuggestionService = new CategorySuggestionService(_context);
        _recommendationService = new RecommendationService(_context);
        
        _mockUserManager = GetMockUserManager();
        _controller = new ProjectController(
            _context,
            _mockUserManager.Object,
            _balanceService.Object,
            _categorySuggestionService,
            _recommendationService);
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
    public async Task Index_WithFilters_ReturnsFilteredProjects()
    {
        SetUser("client1");
        
        var client1 = new IdentityUser { Id = "c1", UserName = "c1@test.com" };
        var client2 = new IdentityUser { Id = "c2", UserName = "c2@test.com" };
        await _context.Users.AddRangeAsync(client1, client2);
        
        _context.Projects.AddRange(
            new Project { Title = "One", Description = "One", ClientId = "c1", Status = ProjectStatus.Open, Budget = 100, CreatedAt = DateTime.UtcNow },
            new Project { Title = "Two", Description = "Two", ClientId = "c2", Status = ProjectStatus.Open, Budget = 200, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var result = await _controller.Index("One", ProjectStatus.Open.ToString(), 50, 300, null!, null, null);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Project>>(view.Model);
        Assert.Single(model);
        Assert.Equal("One", model.First().Title);
    }
    
    [Fact]
    public async Task Index_WithCategoryFilter_ReturnsFilteredProjects()
    {
        SetUser("client1");
        
        var client = new IdentityUser { Id = "c1", UserName = "c1@test.com" };
        await _context.Users.AddAsync(client);

        var cat1 = new Category { Id = 1, Name = "Веб-разработка", IsActive = true, CreatedAt = DateTime.UtcNow };
        var cat2 = new Category { Id = 2, Name = "Дизайн", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.AddRange(cat1, cat2);

        _context.Projects.AddRange(
            new Project { Title = "Web project", Description = "d", ClientId = "c1", Status = ProjectStatus.Open, Budget = 100, CreatedAt = DateTime.UtcNow, Categories = new List<Category> { cat1 } },
            new Project { Title = "Design project", Description = "d", ClientId = "c1", Status = ProjectStatus.Open, Budget = 200, CreatedAt = DateTime.UtcNow, Categories = new List<Category> { cat2 } }
        );
        await _context.SaveChangesAsync();

        var result = await _controller.Index(null, null, null, null, null!, new List<int> { 1 }, null);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Project>>(view.Model);
        Assert.Single(model);
        Assert.Equal("Web project", model.First().Title);
    }

    [Fact]
    public async Task Details_ReturnsView_WhenProjectExists()
    {
        
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "c1" };
        var client = new IdentityUser { Id = "c1" };
        
        _context.Users.Add(client);
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var result = await _controller.Details(1);

        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<Project>(view.Model);
    }
    
    [Fact]
    public async Task Details_ReturnsNotFound_WhenProjectMissing()
    {
        var result = await _controller.Details(999);
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
        SetUser("client1");

        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client1" };
        var client = new IdentityUser { Id = "client1", Email = "client1@test.com" };

        _context.Projects.Add(project);
        _context.Users.Add(client);
        await _context.SaveChangesAsync();

        var dto = new CreateProjectDto { Title = "p1", Description = "p1", Budget = 500, Status = ProjectStatus.Open };
        var result = await _controller.Create(dto);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProjectController.MyProjects), redirect.ActionName);
    }

    [Fact]
    public async Task Create_Post_AutoAssignsCategories_WhenNoneSelected()
    {
        SetUser("client1");

        var client = new IdentityUser { Id = "client1", Email = "client1@test.com" };
        _context.Users.Add(client);

        var cat = new Category { Id = 1, Name = "Веб-разработка", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(cat);
        await _context.SaveChangesAsync();

        var dto = new CreateProjectDto
        {
            Title = "Нужен сайт на React",
            Description = "Создание лендинга",
            Budget = 500,
            Status = ProjectStatus.Open,
            CategoryIds = new List<int>()
        };

        var result = await _controller.Create(dto);

        Assert.IsType<RedirectToActionResult>(result);

        var created = await _context.Projects
            .Include(p => p.Categories)
            .FirstAsync(p => p.Title == "Нужен сайт на React");
        Assert.NotEmpty(created.Categories);
        Assert.Contains(created.Categories, c => c.Name == "Веб-разработка");
    }

    [Fact]
    public async Task Create_Post_AssignsOther_WhenNoKeywordsMatch()
    {
        SetUser("client1");

        var client = new IdentityUser { Id = "client1", Email = "client1@test.com" };
        _context.Users.Add(client);
        
        var catOther = new Category { Id = 1, Name = "Другое", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(catOther);
        await _context.SaveChangesAsync();
        
        var dto = new CreateProjectDto
        {
            Title = "Выгулять собаку",
            Description = "нужно погулять с псом",
            Budget = 500,
            Status = ProjectStatus.Open,
            CategoryIds = new List<int>()
        };

        var result = await _controller.Create(dto);

        Assert.IsType<RedirectToActionResult>(result);

        var created = await _context.Projects
            .Include(p => p.Categories)
            .FirstAsync(p => p.Title == "Выгулять собаку");
        Assert.Single(created.Categories);
        Assert.Equal("Другое", created.Categories.First().Name);
    }
    
    [Fact]
    public async Task Edit_Get_ReturnsView_WhenValid()
    {
        SetUser("client1");
        
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client1" };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var result = await _controller.Edit(1);
        var view = Assert.IsType<ViewResult>(result);
    }
    
    [Fact]
    public async Task Edit_Post_UpdatesProject_WhenValid()
    {
        SetUser("client1");
        
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client1" };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        
        var dto = new UpdateProjectDto { Title = "updated" };
        var result = await _controller.Edit(1, dto);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProjectController.MyProjects), redirect.ActionName);

        var updated = await _context.Projects.FindAsync(1);
        Assert.Equal("updated", updated!.Title);
    }
    
    [Fact]
    public async Task Edit_Post_UpdatesProject_WhenInvalid()
    {
        SetUser("client1");
        
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client1" };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        
        var dto = new UpdateProjectDto { Title = "updated" };
        var result = await _controller.Edit(999, dto);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_AutoAssignsCategories_WhenAllRemoved()
    {
        SetUser("client1");
        var cat = new Category { Id = 1, Name = "Другое", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(cat);

        var project = new Project
        {
            Id = 1,
            Title = "Какой-то проект",
            Description = "Без ключевых слов",
            ClientId = "client1",
            Categories = new List<Category> { cat }
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var dto = new UpdateProjectDto
        {
            Title = "Какой-то проект",
            Description = "Без ключевых слов",
            CategoryIds = new List<int>()
        };
        
        var result = await _controller.Edit(1, dto);

        Assert.IsType<RedirectToActionResult>(result);

        var updated = await _context.Projects
            .Include(p => p.Categories)
            .FirstAsync(p => p.Id == 1);
        Assert.NotEmpty(updated.Categories);
    }
    
    [Fact]
    public async Task Delete_RemovesProject_WhenValid()
    {
        SetUser("client1");
        
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client1" };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        
        var result = await _controller.Delete(1);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ProjectController.MyProjects), redirect.ActionName);
        
        var deleted = await _context.Projects.FindAsync(1);
        Assert.Null(deleted);
    }
    
    [Fact]
    public async Task MyProjects_ReturnsUserProjects()
    {
        SetUser("client1");
        
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        
        var project1 = new Project { Id = 1, Title = "p1", Description = "My description", ClientId = client.Id, CreatedAt = DateTime.UtcNow };
        var project2 = new Project { Id = 2, Title = "p2", Description = "My description", ClientId = client.Id, CreatedAt = DateTime.UtcNow };
        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        var result = await _controller.MyProjects();
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Project>>(view.Model);
        Assert.Equal(2, model.Count());

    }

    [Fact]
    public async Task AcceptBid_SetsBidAccepted_WhenValid()
    {
        SetUser("client1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", Status = BidStatus.Pending };
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client1" };
        _context.Projects.Add(project);
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        
        var result = await _controller.AcceptBid(1, 1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(BidStatus.Accepted, _context.Bids.Find(1)!.Status);
    }
    
    [Fact]
    public async Task AcceptBid_SetsBidAccepted_NotFound()
    {
        SetUser("client1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", Status = BidStatus.Pending };
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client1" };
        _context.Projects.Add(project);
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        
        var result = await _controller.AcceptBid(5, 5);

        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task AcceptBid_SetsBidAccepted_Forbid()
    {
        SetUser("client1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", Status = BidStatus.Pending };
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client2" };
        _context.Projects.Add(project);
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        
        var result = await _controller.AcceptBid(1, 1);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task RejectedBid_SetsBidRejected()
    {
        SetUser("client1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", Status = BidStatus.Pending };
        var project = new Project { Id = 1, Title = "p1", Description = "p1", ClientId = "client1" };
        _context.Projects.Add(project);
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        
        var result = await _controller.RejectBid(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(BidStatus.Rejected, _context.Bids.Find(1)!.Status);
    }

    [Fact]
    public async Task CompleteProject_UpdatesStatus()
    {
        SetUser("freelancer1");
    
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;

        // Подготовка балансов для BalanceService
        _context.UserBalances.AddRange(
            new UserBalance { UserId = "client1", Balance = 0, Frozen = 100 },
            new UserBalance { UserId = "freelancer1", Balance = 0, Frozen = 0 }
        );

        var project = new Project
        {
            Id = 1, 
            Title = "p1", 
            Description = "p1", 
            Status = ProjectStatus.Paid, 
            ClientId = "client1", 
            SelectedFreelancerId = "freelancer1"
        };

        var bid = new Bid
        {
            Id = 1,
            ProjectId = 1,
            FreelancerId = "freelancer1",
            Status = BidStatus.Accepted,
            Amount = 100
        };

        _context.Bids.Add(bid);
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
    
        var result = await _controller.CompleteProject(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ProjectStatus.Completed, _context.Projects.Find(1)!.Status);
    }

    [Fact]
    public async Task CancelProject_UpdatesStatus()
    {
        SetUser("client1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var project = new Project { Id = 1, Title = "p1", Description = "p1", Status = ProjectStatus.Open, ClientId = "client1" };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        
        var result = await _controller.CancelProject(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ProjectStatus.Cancelled, _context.Projects.Find(1)!.Status);
    }

    [Fact]
    public async Task ResumeProject_UpdatesStatus_WhenValid()
    {
        SetUser("client1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };  
        var project = new Project { Id = 1, Title = "p1", Description = "p1", Status = ProjectStatus.Cancelled, ClientId = "client1" };
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", Status = BidStatus.Accepted };

        _context.Users.Add(client);
        _context.Projects.Add(project);
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        
        var result = await _controller.ResumeProject(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ProjectStatus.Open, _context.Projects.Find(1)!.Status);
    }
    
    [Fact]
    public async Task ResumeProject_UpdatesStatus_NotFound()
    {
        SetUser("client1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };  
        var project = new Project { Id = 1, Title = "p1", Description = "p1", Status = ProjectStatus.Cancelled, ClientId = "client1" };
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", Status = BidStatus.Accepted };

        _context.Users.Add(client);
        _context.Projects.Add(project);
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        
        var result = await _controller.ResumeProject(999);

        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task ResumeProject_UpdatesStatus_BadRequest()
    {
        SetUser("client1");
        
        var tempDataMock = new Mock<ITempDataDictionary>();
        _controller.TempData = tempDataMock.Object;
        
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };  
        var project = new Project { Id = 1, Title = "p1", Description = "p1", Status = ProjectStatus.InProgress, ClientId = "client1" };
        var bid = new Bid { Id = 1, ProjectId = 1, FreelancerId = "freelancer1", Status = BidStatus.Accepted };

        _context.Users.Add(client);
        _context.Projects.Add(project);
        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        
        var result = await _controller.ResumeProject(1);

        Assert.IsType<BadRequestObjectResult>(result);
    }
    
    [Fact]
    public async Task SuggestCategories_ReturnsSuggestedIds()
    {
        SetUser("client1");

        var cat = new Category { Id = 1, Name = "Веб-разработка", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(cat);
        await _context.SaveChangesAsync();

        var request = new SuggestCategoriesRequest { Title = "Нужен сайт", Description = "на React" };
        var result = await _controller.SuggestCategories(request);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var ids = Assert.IsAssignableFrom<List<int>>(jsonResult.Value);
        Assert.Contains(1, ids);
    }

    [Fact]
    public async Task SuggestCategories_ReturnsOther_WhenNoMatch()
    {
        SetUser("client1");

        var catOther = new Category { Id = 1, Name = "Другое", IsActive = true, CreatedAt = DateTime.UtcNow };
        _context.Categories.Add(catOther);
        await _context.SaveChangesAsync();

        var request = new SuggestCategoriesRequest { Title = "Выгулять собаку", Description = "" };
        var result = await _controller.SuggestCategories(request);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var ids = Assert.IsAssignableFrom<List<int>>(jsonResult.Value);
        Assert.Single(ids);
        Assert.Contains(1, ids);
    }

    [Fact]
    public async Task SuggestCategories_ReturnsEmpty_WhenRequestNull()
    {
        SetUser("client1");

        var result = await _controller.SuggestCategories(null!);

        var jsonResult = Assert.IsType<JsonResult>(result);
        var ids = Assert.IsAssignableFrom<List<int>>(jsonResult.Value);
        Assert.Empty(ids);
    }
}