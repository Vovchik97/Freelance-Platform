using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Web;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FreelancePlatform.FreelancePlatform.Tests.Web;

public class ChatControllerTests
{
    private readonly AppDbContext _context;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly ChatController _controller;

    public ChatControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        
        _mockUserManager = GetMockUserManager();
        _controller = new ChatController(_context, _mockUserManager.Object);
    }

    private static Mock<UserManager<IdentityUser>> GetMockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        var mock = new Mock<UserManager<IdentityUser>>(
            store.Object, null, null, null, null, null, null, null, null);

        mock.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((ClaimsPrincipal principal) =>
            {
                return principal.FindFirstValue(ClaimTypes.NameIdentifier);
            });
        
        mock.Setup(m => m.FindByIdAsync("freelancer1"))
            .ReturnsAsync(new IdentityUser { Id = "freelancer1", UserName = "freelancer1@test.com" });

        mock.Setup(m => m.FindByIdAsync("freelancer2"))
            .ReturnsAsync(new IdentityUser { Id = "freelancer2", UserName = "freelancer2@test.com" });

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
    public async Task Index_ReturnsChatViewModelList()
    {
        SetUser("client1");
        
        _context.Chats.Add(new Chat
        {
            Id = 1,
            ClientId = "client1",
            FreelancerId = "freelancer1",
            Messages = new List<Message>
            {
                new Message { Id = 1, SenderId = "freelancer1", IsRead = false, Text = "test" }
            }
        });
        await _context.SaveChangesAsync();

        var result = await _controller.Index();
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<ChatViewModel>>(viewResult.Model);
        Assert.Single(model);
        Assert.True(model[0].HasUnread);
        Assert.Equal("freelancer1@test.com", model[0].OtherUserName);
    }

    [Fact]
    public async Task Chat_ReturnsView_WhenUserOwnsChat()
    {
        SetUser("client1");
        
        var chat = new Chat
        {
            Id = 1,
            ClientId = "client1",
            FreelancerId = "freelancer1",
            Messages = new List<Message>
            {
                new Message { Id = 1, SenderId = "freelancer1", IsRead = false, SentAt = DateTime.Now, Text = "test" }
            }
        };
        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        var result = await _controller.Chat(1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var returnedChat = Assert.IsType<Chat>(viewResult.Model);
        Assert.Equal(1, returnedChat.Id);
        Assert.All(returnedChat.Messages, m => Assert.True(m.IsRead));
    }

    [Fact]
    public async Task Chat_ReturnsNotFound_WhenUserIsNotParticipant()
    {
        SetUser("client1");
        
        var chat = new Chat
        {
            Id = 1,
            ClientId = "client5",
            FreelancerId = "freelancer1",
            Messages = new List<Message>()
        };
        _context.Chats.Add(chat);
        await _context.SaveChangesAsync();

        var result = await _controller.Chat(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetUnreadChatsCount_ReturnsCorrectCount()
    {
        SetUser("client1");
        
        _context.Chats.Add(new Chat
        {
            Id = 1,
            ClientId = "client1",
            FreelancerId = "freelancer1",
            Messages = new List<Message>
            {
                new Message { Id = 1, SenderId = "freelancer1", IsRead = false, Text = "test" }
            }
        });
        _context.Chats.Add(new Chat
        {
            Id = 2,
            ClientId = "client1",
            FreelancerId = "freelancer2",
            Messages = new List<Message>
            {
                new Message { Id = 2, SenderId = "freelancer2", IsRead = true, Text = "test" }
            }
        });
        await _context.SaveChangesAsync();

        var result = await _controller.GetUnreadChatsCount();
        var json = Assert.IsType<JsonResult>(result);
        dynamic data = json.Value!;
        Assert.Equal(1, (int)data.count);
    }

    [Fact]
    public async Task GetUnreadChatsCount_ReturnsUnauthorized_IfUserNull()
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // no user
        };

        var result = await _controller.GetUnreadChatsCount();

        Assert.IsType<UnauthorizedResult>(result);
    }
}