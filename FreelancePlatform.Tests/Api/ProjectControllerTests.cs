using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FreelancePlatform.Controllers.Api;
using FreelancePlatform.Context;
using FreelancePlatform.Dto.Projects;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Api;

public class ProjectControllerTests
{
    private readonly AppDbContext _context;
    private readonly ProjectController _controller;

    public ProjectControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _controller = new ProjectController(_context);
    }

    [Fact]
    public async Task CreateProject_ReturnsCreatedAtAction_WhenValid()
    {
        var dto = new CreateProjectDto
        {
            Title = "New Project",
            Description = "Desc",
            Budget = 1000,
            Status = ProjectStatus.Open
        };

        var userId = "client1";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        var result = await _controller.CreateProject(dto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var project = Assert.IsType<Project>(createdResult.Value);
        Assert.Equal(dto.Title, project.Title);
        Assert.Equal(userId, project.ClientId);
    }

    [Fact]
    public async Task UpdateProject_ReturnsForbid_WhenUserNotOwner()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        
        var project = new Project
        {
            Id = 1,
            Title = "Old",
            Description = "My project",
            ClientId = "client1",
            Status = ProjectStatus.Open,
            Budget = 1000,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var dto = new UpdateProjectDto
        {
            Title = "New",
            Description = "New desc",
            Budget = 2000,
            Status = ProjectStatus.Completed
        };

        var userId = "otherClient";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        var result = await _controller.UpdateProject(1, dto);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task UpdateProject_ReturnsOk_WhenUserIsOwner()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        
        var project = new Project
        {
            Id = 2,
            Title = "Initial",
            Description = "My project",
            ClientId = "client1",
            Status = ProjectStatus.Open,
            Budget = 1500,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var dto = new UpdateProjectDto
        {
            Title = "Updated",
            Description = "Updated desc",
            Budget = 2500,
            Status = ProjectStatus.InProgress
        };

        var userId = "client1";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        var result = await _controller.UpdateProject(2, dto);

        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
        
        Assert.Equal(dto.Title, result.Value.Title);
        Assert.Equal(dto.Description, result.Value.Description);
        Assert.Equal(dto.Budget, result.Value.Budget);
        Assert.Equal(dto.Status, result.Value.Status);
    }

    [Fact]
    public async Task GetProjectById_ReturnsProject_WhenExists()
    {
        var client = new IdentityUser { Id = "client1", UserName = "client1@test.com" };
        await _context.Users.AddAsync(client);
        
        var project = new Project
        {
            Id = 1,
            Title = "Test",
            Description = "My project",
            ClientId = "client1",
            Status = ProjectStatus.Open,
            Budget = 500,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var result = await _controller.GetProject(1);

        Assert.Null(result.Result);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.Id);
    }

    [Fact]
    public async Task DeleteProject_ReturnsForbid_WhenUserNotOwner()
    {
        var project = new Project
        {
            Id = 4,
            Title = "DeleteTest",
            Description = "My project",
            ClientId = "client4",
            Status = ProjectStatus.Open,
            Budget = 700,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var userId = "otherUser";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        var result = await _controller.DeleteProject(4);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DeleteProject_ReturnsNoContent_WhenUserIsOwner()
    {
        var project = new Project
        {
            Id = 5,
            Title = "DeleteMine",
            Description = "My project",
            ClientId = "client5",
            Status = ProjectStatus.Open,
            Budget = 900,
            CreatedAt = DateTime.UtcNow
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var userId = "client5";
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        var result = await _controller.DeleteProject(5);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetProjects_ReturnsFilteredByStatus()
    {
        var client1 = new IdentityUser { Id = "c1", UserName = "c1@test.com" };
        var client2 = new IdentityUser { Id = "c2", UserName = "c2@test.com" };
        await _context.Users.AddRangeAsync(client1, client2);
        
        _context.Projects.AddRange(
            new Project { Title = "One", Description = "One", ClientId = "c1", Status = ProjectStatus.Open, Budget = 100, CreatedAt = DateTime.UtcNow },
            new Project { Title = "Two", Description = "Two", ClientId = "c2", Status = ProjectStatus.Completed, Budget = 200, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var result = await _controller.ListProjects(null, status: ProjectStatus.Open.ToString(), null, null, null);
        var projects = Assert.IsAssignableFrom<IEnumerable<Project>>(result.Value);
        Assert.Single(projects);
        Assert.Equal(ProjectStatus.Open, projects.First().Status);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
