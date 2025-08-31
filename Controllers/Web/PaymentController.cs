using FreelancePlatform.Context;
using FreelancePlatform.Dto.Payment;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

[Authorize(Roles = "Client")]
public class PaymentController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IPaymentProvider _paymentProvider;

    public PaymentController(AppDbContext context, UserManager<IdentityUser> userManager, IPaymentProvider paymentProvider)
    {
        _context = context;
        _userManager = userManager;
        _paymentProvider = paymentProvider;
    }
    
    // страница подтверждения оплаты
    [HttpGet]
    public async Task<IActionResult> Create(int orderId)
    {
        var userId = _userManager.GetUserId(User);
        var order = await _context.Orders
            .Include(o => o.Service)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            return NotFound();
        }

        if (order.ClientId != userId)
        {
            return Forbid();
        }

        if (order.Status != OrderStatus.Accepted)
        {
            return BadRequest();
        }

        var dto = new PaymentCreateDto
        {
            OrderId = order.Id,
            ServiceTitle = order.Service!.Title,
            Amount = order.Service.Price,
            Currency = "RUB"
        };

        return View(dto);
    }
    
    // создаем Payment в БД и Stripe Session, редиректим на Stripe
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int orderId)
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.GetUserAsync(User);
        var order = await _context.Orders
            .Include(o => o.Service)
            .FirstOrDefaultAsync(o => o.Id == orderId);
        
        if (order == null)
        {
            return NotFound();
        }

        if (order.ClientId != userId)
        {
            return Forbid();
        }
        
        if (order.Status != OrderStatus.Accepted)
        {
            return BadRequest();
        }
        
        var amountMinor = (long)(order.Service!.Price * 100m);
        
        var payment = new Payment
        {
            OrderId = order.Id,
            PayerId = user!.Id,
            AmountMinor = amountMinor,
            Currency = "RUB",
            Provider = "Stripe",
            Status = PaymentStatus.Pending
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        
        var req = new CreateCheckoutSessionRequest
        {
            AmountMinor = amountMinor,
            Currency = "RUB",
            Description = $"Оплата заказа #{order.Id}  {order.Service.Title}",
            CustomerEmail = user?.Email ?? "",
            SuccessUrl = Url.Action(nameof(Success), "Payment", null, Request.Scheme)!,
            CancelUrl = Url.Action(nameof(Cancel), "Payment", new { orderId }, Request.Scheme)!,
            MetadataPaymentId = payment.Id.ToString()
        };
        
        var session = await _paymentProvider.CreateCheckoutSessionAsync(req);

        payment.ProviderSessionId = session.SessionId;
        payment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        return Redirect(session.SessionUrl);
    }
    
    // success URL - подтверждаем у провайдера статус и проставляем в БД
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Success(string session_id)
    {
        if (string.IsNullOrWhiteSpace(session_id))
        {
            return BadRequest("session_id is missing");
        }

        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.ProviderSessionId == session_id);

        if (payment == null)
        {
            return NotFound();
        }

        var (status, pi) = await _paymentProvider.GetSessionStatusAsync(session_id);
        payment.ProviderPaymentIntentId = pi;
        payment.UpdatedAt = DateTime.UtcNow;

        switch (status)
        {
            case ExternalPaymentsStatus.Succeeded:
                payment.Status = PaymentStatus.Succeeded;

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                if (order != null && order.Status == OrderStatus.Accepted)
                {
                    order.Status = OrderStatus.Paid;
                }
                break;
            
            case ExternalPaymentsStatus.Canceled:
                payment.Status = PaymentStatus.Canceled;
                break;
            
            case ExternalPaymentsStatus.Failed:
                payment.Status = PaymentStatus.Failed;
                break;
            
            default:
                break;
        }

        await _context.SaveChangesAsync();
        return View("Success", payment);
    }
    
    // cancel URL - покупатель вернулся с отменой
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Cancel(int orderId)
    {
        var payment = await _context.Payments
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        if (payment != null && payment.Status == PaymentStatus.Pending)
        {
            payment.Status = PaymentStatus.Canceled;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return View("Cancel", payment);
    }

    [HttpGet]
    public async Task<IActionResult> My()
    {
        var userId = _userManager.GetUserId(User);
        var items = await _context.Payments
            .Include(p => p.Order)
            .ThenInclude(o => o!.Service)
            .Where(p => p.PayerId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(items);
    }
}