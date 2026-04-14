using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Web;
using FreelancePlatform.Dto.TeamProject;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FreelancePlatform.FreelancePlatform.Tests.Web;

public class TeamProjectControllerTests
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly Mock<ProjectActivityLogService> _mockLogService;
    private readonly Mock<BalanceService> _mockBalanceService;
    private readonly TeamProjectController _controller;

    public TeamProjectControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _mockUserManager = GetMockUserManager();
        _mockLogService = new Mock<ProjectActivityLogService>(_context);
        
        var balanceService = new BalanceService(_context);

        _controller = new TeamProjectController(
            _context,
            _mockUserManager.Object,
            _mockLogService.Object,
            balanceService);

        var tempData = new Mock<ITempDataDictionary>();
        _controller.TempData = tempData.Object;
    }

    private static Mock<UserManager<IdentityUser>> GetMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        var mock = new Mock<UserManager<IdentityUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        mock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((ClaimsPrincipal p) => p.FindFirstValue(ClaimTypes.NameIdentifier));

        return mock;
    }

    private void SetUser(string userId, string role = "Client")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Details_ReturnsNotFound_WhenProjectMissing()
    {
        SetUser("client1");
        var result = await _controller.Details(999);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_ReturnsView_ForClient()
    {
        var client = new IdentityUser
        {
            Id = "client1",
            UserName = "client1",
            NormalizedUserName = "CLIENT1"
        };

        _context.Users.Add(client);
        await _context.SaveChangesAsync();

        _mockUserManager.Setup(m => m.Users).Returns(_context.Users);
        
        SetUser("client1");
        var project = new Project
        {
            Id = 1, 
            Title = "T", 
            Description = "D",
            Client = client,
            ClientId = "client1", 
            IsTeamProject = true,
            Members = new List<ProjectMember>(),
            Tasks = new List<ProjectTask>(),
            Bids = new List<Bid>(),
            GroupChatMessages = new List<GroupChatMessage>(),
            ActivityLogs = new List<ProjectActivityLog>
            {
                new ProjectActivityLog
                {
                    Id = 1,
                    ProjectId = 1,
                    Action = "test",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var result = await _controller.Details(1);
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task InviteMember_ReturnsForbid_whenNoClientOrLead()
    {
        SetUser("client2");
        var project = new Project
        {
            Id = 1, Title = "T", Description = "D",
            ClientId = "client1", IsTeamProject = true
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        
        var result = await _controller.InviteMember(new InviteMemberDto() { ProjectId = 1, UserName = "user2" });
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task InviteMember_AddsMember_WhenValid()
    {
        SetUser("client1");

        var target = new IdentityUser { Id = "freelancer1", UserName = "freelancer1" };
        _mockUserManager
            .Setup(m => m.FindByNameAsync("freelancer1"))
            .ReturnsAsync(target);
        _mockUserManager
            .Setup(m => m.Users)
            .Returns(new List<IdentityUser> { target }.AsQueryable());

        var project = new Project
        {
            Id = 1, Title = "T", Description = "D",
            ClientId = "client1", IsTeamProject = true
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var result = await _controller.InviteMember(
            new InviteMemberDto { ProjectId = 1, UserName = "freelancer1" });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(await _context.ProjectMembers.AnyAsync(m => m.UserId == "freelancer1"));
    }

    [Fact]
    public async Task AcceptInvite_UpdatesStatus_WhenValid()
    {
        SetUser("freelancer1");

        var project = new Project
        {
            Id = 1, Title = "T", Description = "D",
            ClientId = "client1", Status = ProjectStatus.Open
        };

        var member = new ProjectMember
        {
            Id = 1, ProjectId = 1, UserId = "freelancer1",
            UserName = "freelancer1", Status = ProjectMemberStatus.Pending,
            Project = project
        };
        _context.Projects.Add(project);
        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync();

        var result = await _controller.AcceptInvite(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ProjectMemberStatus.Accepted,
            (await _context.ProjectMembers.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task DeclineInvite_UpdatesStatus_WhenValid()
    {
        SetUser("freelancer1");

        var project = new Project
        {
            Id = 1, Title = "T", Description = "D", ClientId = "client1"
        };
        var member = new ProjectMember
        {
            Id = 1, ProjectId = 1, UserId = "freelancer1",
            UserName = "freelancer1", Status = ProjectMemberStatus.Pending,
            Project = project
        };
        _context.Projects.Add(project);
        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync();

        var result = await _controller.DeclineInvite(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ProjectMemberStatus.Rejected,
            (await _context.ProjectMembers.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task RemoveMember_RemovesMember_WhenValid()
    {
        SetUser("client1");

        var project = new Project
        {
            Id = 1, Title = "T", Description = "D",
            ClientId = "client1", IsTeamProject = true
        };
        var member = new ProjectMember
        {
            Id = 1, ProjectId = 1, UserId = "freelancer1",
            UserName = "f1", Status = ProjectMemberStatus.Accepted,
            Project = project
        };
        _context.Projects.Add(project);
        _context.ProjectMembers.Add(member);
        await _context.SaveChangesAsync();

        var result = await _controller.RemoveMember(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.False(await _context.ProjectMembers.AnyAsync(m => m.Id == 1));
    }

    [Fact]
    public async Task CreateTask_ReturnsNotFound_WhenProjectMissing()
    {
        SetUser("client1");
        var result = await _controller.CreateTask(new CreateTaskDto { ProjectId = 999, Title = "T" });
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateTask_CreatesTask_WhenValid()
    {
        SetUser("client1");

        var project = new Project
        {
            Id = 1, Title = "T", Description = "D",
            ClientId = "client1", Status = ProjectStatus.InProgress,
            IsTeamProject = true
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var result = await _controller.CreateTask(new CreateTaskDto { ProjectId = 1, Title = "Task1" });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.True(await _context.ProjectTasks.AnyAsync(t => t.Title == "Task1"));
    }

    [Fact]
    public async Task UpdateTaskStatus_ReturnsNotFound_WhenTaskMissing()
    {
        SetUser("freelancer1");
        var result = await _controller.UpdateTaskStatus(999, (int)ProjectTaskStatus.InProgress);
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateTaskStatus_UpdatesStatus_WhenValid()
    {
        SetUser("freelancer1", "Freelancer");

        var project = new Project
        {
            Id = 1, Title = "T", Description = "D",
            ClientId = "client1", Status = ProjectStatus.InProgress
        };

        var member = new ProjectMember
        {
            UserId = "freelancer1", UserName = "f1",
            Role = ProjectMemberRole.Member,
            Status = ProjectMemberStatus.Accepted,
            Project = project, ProjectId = 1
        };
        var task = new ProjectTask
        {
            Id = 1, ProjectId = 1, Title = "Task",
            AssignedToUserId = "freelancer1",
            Status = ProjectTaskStatus.Todo,
            Project = project
        };
        project.Members = new List<ProjectMember> { member };
        _context.Projects.Add(project);
        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync();

        var result = await _controller.UpdateTaskStatus(1, (int)ProjectTaskStatus.InProgress);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ProjectTaskStatus.InProgress, (await _context.ProjectTasks.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task DeleteTask_DeletesTask_WhenClient()
    {
        SetUser("client1");

        var project = new Project
        {
            Id = 1, Title = "T", Description = "D",
            ClientId = "client1", Status = ProjectStatus.InProgress
        };
        var task = new ProjectTask
        {
            Id = 1, ProjectId = 1, Title = "Task",
            Status = ProjectTaskStatus.Todo, Project = project
        };
        _context.Projects.Add(project);
        _context.ProjectTasks.Add(task);
        await _context.SaveChangesAsync();
        
        var result = await _controller.DeleteTask(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.False(await _context.ProjectTasks.AnyAsync(t => t.Id == 1));
    }

    [Fact]
    public async Task ConfirmCompletion_ReturnsNotFound_WhenProjectMissing()
    {
        SetUser("client1");
        var result = await _controller.ConfirmCompletion(
            999, new List<string>(), new List<string>());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ConfirmCompletion_CompletesProject_WhenValid()
    {
        SetUser("client1");

        var project = new Project
        {
            Id = 1, Title = "T", Description = "D",
            ClientId = "client1", Status = ProjectStatus.InProgress,
            Budget = 1000, IsTeamProject = true
        };
        var member = new ProjectMember
        {
            UserId = "freelancer1", UserName = "f1",
            Role = ProjectMemberRole.Member,
            Status = ProjectMemberStatus.Accepted,
            ProjectId = 1, Project = project
        };
        project.Members = new List<ProjectMember> { member };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        _context.UserBalances.AddRange(
            new UserBalance { UserId = "client1", Balance = 0, Frozen = 1000 },
            new UserBalance { UserId = "freelancer1", Balance = 0, Frozen = 0 }
        );

        var result = await _controller.ConfirmCompletion(
            1,
            new List<string> { "freelancer1" },
            new List<string> { "1000,00" });

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ProjectStatus.Completed,
            (await _context.Projects.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task CancelProject_UpdatesStatus_WhenOpen()
    {
        SetUser("client1");

        var project = new Project
        {
            Id = 1, Title = "T", Description = "D",
            ClientId = "client1", Status = ProjectStatus.Open
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        var result = await _controller.CancelProject(1);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(ProjectStatus.Cancelled, (await _context.Projects.FindAsync(1))!.Status);
    }

    [Fact]
    public async Task CancelProject_ReturnsNotFound_WhenMissing()
    {
        SetUser("client1");
        var result = await _controller.CancelProject(999);
        Assert.IsType<NotFoundResult>(result);
    }
}