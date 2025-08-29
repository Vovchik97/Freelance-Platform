using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Web;
using FreelancePlatform.Dto.Payment;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FreelancePlatform.FreelancePlatform.Tests.Web;

public class PaymentControllerTests
{
    private AppDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private Mock<UserManager<IdentityUser>> GetUserManagerMock()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        var mgr = new Mock<UserManager<IdentityUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!,
            null!);
        
        mgr.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>()))
            .Returns((ClaimsPrincipal u) => u.FindFirstValue(ClaimTypes.NameIdentifier)!);

        mgr.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new IdentityUser { Id = "test-user", Email = "test@example.com" });

        return mgr;
    }
    
    private void SetUser(Controller controller, string userId, string userName = "testUser")
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userName)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task Create_ReturnsNotFound_WhenOrderMissing()
    {
        using var context = GetDbContext();
        var userManager = GetUserManagerMock();
        var paymentProvider = new Mock<IPaymentProvider>();
        var controller = new PaymentController(context, userManager.Object, paymentProvider.Object);
        SetUser(controller, "test-user");
        
        var result = await controller.Create(1);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsForbid_WhenNotOwner()
    {
        using var context = GetDbContext();
        context.Orders.Add(new Order
        {
            Id = 1,
            ClientId = "other",
            Status = OrderStatus.Accepted,
            Service = new Service
            {
                Id = 1,
                Title = "S1",
                Price = 100,
                Description = "S1 description",
                FreelancerId = "f1"
            },
            ServiceId = 1
        });
        await context.SaveChangesAsync();
        
        var userManager = GetUserManagerMock();
        var controller = new PaymentController(context, userManager.Object, new Mock<IPaymentProvider>().Object);
        SetUser(controller, "test-user");
        
        var result = await controller.Create(1);

        Assert.IsType<ForbidResult>(result);
    }
    
    [Fact]
    public async Task Create_ReturnsBadRequest_WhenNotAcceptedOrder()
    {
        using var context = GetDbContext();
        context.Orders.Add(new Order
        {
            Id = 1,
            ClientId = "test-user",
            Status = OrderStatus.Pending,
            Service = new Service
            {
                Id = 1,
                Title = "S1",
                Price = 100,
                Description = "S1 description",
                FreelancerId = "f1"
            },
            ServiceId = 1
        });
        await context.SaveChangesAsync();
        
        var userManager = GetUserManagerMock();
        var controller = new PaymentController(context, userManager.Object, new Mock<IPaymentProvider>().Object);
        SetUser(controller, "test-user");
        
        var result = await controller.Create(1);

        Assert.IsType<BadRequestResult>(result);
    }

    [Fact]
    public async Task Create_ReturnsView_WithDto_WhenAccepted()
    {
        using var context = GetDbContext();
        var order = new Order
        {
            Id = 1,
            ClientId = "test-user",
            Status = OrderStatus.Accepted,
            Service = new Service
            {
                Id = 1,
                Title = "S1",
                Price = 100,
                Description = "S1 description",
                FreelancerId = "f1"
            },
            ServiceId = 1
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        
        var userManager = GetUserManagerMock();
        var controller = new PaymentController(context, userManager.Object, new Mock<IPaymentProvider>().Object);
        SetUser(controller, "test-user");
        
        var result = await controller.Create(1);

        var view = Assert.IsType<ViewResult>(result);
        var dto = Assert.IsType<PaymentCreateDto>(view.Model);
        Assert.Equal(order.Id, dto.OrderId);
        Assert.Equal(order.Service.Title, dto.ServiceTitle);
    }

    [Fact]
    public async Task Success_UpdatesPayment_WhenSucceeded()
    {
        using var context = GetDbContext();
        var userManager = GetUserManagerMock();
        var providerMock = new Mock<IPaymentProvider>();
        providerMock.Setup(p => p.GetSessionStatusAsync("sess123"))
            .ReturnsAsync((ExternalPaymentsStatus.Succeeded, "pi_123"));

        var controller = new PaymentController(context, userManager.Object, providerMock.Object);
        SetUser(controller, "test-user");
        
        var order = new Order
        {
            Id = 1,
            ClientId = "test-user",
            Status = OrderStatus.Accepted,
            Service = new Service
            {
                Id = 1,
                Title = "S1",
                Price = 100,
                Description = "S1 description",
                FreelancerId = "f1"
            },
            ServiceId = 1
        };
        var payment = new Payment
        {
            Id = 1, 
            OrderId = 1, 
            ProviderSessionId = "sess123", 
            Status = PaymentStatus.Pending,
            PayerId = "test-user",
        };
        context.Orders.Add(order);
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        
        
        var result = await controller.Success("sess123");

        var view = Assert.IsType<ViewResult>(result);
        var updatedPayment = Assert.IsType<Payment>(view.Model);
        Assert.Equal(PaymentStatus.Succeeded, updatedPayment.Status);

        var updatedOrder = await context.Orders.FirstAsync();
        Assert.Equal(OrderStatus.Paid, updatedOrder.Status);
    }

    [Fact]
    public async Task Cancel_SetsPaymentCanceled_WhenPending()
    {
        using var context = GetDbContext();
        var payment = new Payment { Id = 1, OrderId = 10, Status = PaymentStatus.Pending, PayerId = "test-user" };
        context.Payments.Add(payment);
        await context.SaveChangesAsync();
        
        var userManager = GetUserManagerMock();
        var controller = new PaymentController(context, userManager.Object, new Mock<IPaymentProvider>().Object);
        SetUser(controller, "test-user");
        
        var result = await controller.Cancel(10);

        var view = Assert.IsType<ViewResult>(result);
        var updated = Assert.IsType<Payment>(view.Model);
        Assert.Equal(PaymentStatus.Canceled, updated.Status);
    }

    [Fact]
    public async Task My_ReturnsOnlyUserPayments()
    {
        using var context = GetDbContext();
        context.Payments.Add(new Payment
        {
            Id = 1,
            PayerId = "test-user",
            Status = PaymentStatus.Succeeded,
            Order = new Order
            {
                Service = new Service
                {
                    Title = "S1",
                    Description = "desc",
                    FreelancerId = "f1"
                },
                ServiceId = 0,
                ClientId = "c1"
            }
        });
        context.Payments.Add(new Payment
        {
            Id = 2,
            PayerId = "other",
            Status = PaymentStatus.Succeeded
        });
        await context.SaveChangesAsync();
        
        var userManager = GetUserManagerMock();
        var controller = new PaymentController(context, userManager.Object, new Mock<IPaymentProvider>().Object);
        SetUser(controller, "test-user");
        
        var result = await controller.My();

        var view = Assert.IsType<ViewResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<Payment>>(view.Model);
        Assert.Single(items);
        Assert.Equal("test-user", items.First().PayerId);
    }
}