using FreelancePlatform.Context;
using FreelancePlatform.Dto.Payment;
using FreelancePlatform.Models;
using FreelancePlatform.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

[Authorize(Roles = "Client, Freelancer")]
public class PaymentController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IBalanceService _balanceService;

    public PaymentController(AppDbContext context, UserManager<IdentityUser> userManager, IPaymentProvider paymentProvider, IBalanceService balanceService)
    {
        _context = context;
        _userManager = userManager;
        _paymentProvider = paymentProvider;
        _balanceService = balanceService;
    }
    
    // страница подтверждения оплаты
    [HttpGet]
    public async Task<IActionResult> Create(int? orderId, int? projectId)
    {
        var userId = _userManager.GetUserId(User);

        if (orderId.HasValue)
        {
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
                Title = order.Service!.Title,
                Amount = order.Service.Price,
                Currency = "RUB",
                Type = PaymentType.Order
            };

            return View(dto);
        }

        else if (projectId.HasValue)
        {
            var project = await _context.Projects
                .Include(p => p.Bids)
                .FirstOrDefaultAsync(p => p.Id == projectId);

            var acceptBid = await _context.Bids
                .FirstOrDefaultAsync(b => b.ProjectId == projectId && b.Status == BidStatus.Accepted);

            if (project == null)
            {
                return NotFound();
            }

            if (project.ClientId != userId)
            {
                return Forbid();
            }

            if (project.Status != ProjectStatus.InProgress)
            {
                return BadRequest();
            }

            var dto = new PaymentCreateDto
            {
                ProjectId = project.Id,
                Title = project.Title,
                Amount = acceptBid.Amount,
                Currency = "RUB",
                Type = PaymentType.Project
            };

            return View(dto);
        }

        else
        {
            return BadRequest("Не указан ни orderId, ни projectId");
        }
    }
    
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int? orderId, int? projectId)
    {
        var userId = _userManager.GetUserId(User);

        if (orderId.HasValue)
        {
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

            try
            {
                await _balanceService.FreezeForOrderAsync(
                    userId,
                    order.Service!.Price,
                    order.Id
                );
            }
            catch (InvalidOperationException exception)
            {
                TempData["ErrorMessage"] = exception.Message;
                return RedirectToAction("Details", "Order", new { id = order.Id });
            }

            order.Status = OrderStatus.Paid;

            _context.Payments.Add(new Payment
            {
                OrderId = order.Id,
                PayerId = userId,
                AmountMinor = (long)(order.Service.Price * 100),
                Currency = "RUB",
                Provider = "Internal",
                Status = PaymentStatus.Succeeded,
                Type = PaymentType.Order
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Заказ успешно оплачен";

            return RedirectToAction("MyOrders", "Order");
        }

        if (projectId.HasValue)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);
            
            var acceptBid = await _context.Bids
                .FirstOrDefaultAsync(b => b.ProjectId == projectId && b.Status == BidStatus.Accepted);
            
            if (project == null)
            {
                return NotFound();
            }

            if (project.ClientId != userId)
            {
                return Forbid();
            }
        
            if (project.Status != ProjectStatus.InProgress)
            {
                return BadRequest();
            }
        
            try
            {
                await _balanceService.FreezeForProjectAsync(
                    userId,
                    acceptBid.Amount,
                    project.Id
                );
            }
            catch (InvalidOperationException exception)
            {
                TempData["ErrorMessage"] = exception.Message;
                return RedirectToAction("Details", "Project", new { id = project.Id });
            }

            project.Status = ProjectStatus.Paid;

            _context.Payments.Add(new Payment
            {
                ProjectId = project.Id,
                PayerId = userId,
                AmountMinor = (long)(acceptBid.Amount * 100),
                Currency = "RUB",
                Provider = "Internal",
                Status = PaymentStatus.Succeeded,
                Type = PaymentType.Project
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Проект успешно оплачен";
            
            return RedirectToAction("MyProjects", "Project");
        } 
        
        return BadRequest("Не указан ни orderId, ни projectId");
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
            .Include(o => o.Order)
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.ProviderSessionId == session_id);

        if (payment == null)
        {
            return NotFound();
        }

        var (status, pi) = await _paymentProvider.GetSessionStatusAsync(session_id);
        payment.ProviderPaymentIntentId = pi;
        payment.UpdatedAt = DateTime.UtcNow;

        if (status == ExternalPaymentsStatus.Succeeded &&
            payment.Status != PaymentStatus.Succeeded)
        {
            payment.Status = PaymentStatus.Succeeded;
            
            var amount = payment.AmountMinor / 100m;

            switch (payment.Type)
            {
                case PaymentType.Deposit:
                    await _balanceService.DepositAsync(payment.PayerId, amount, payment.Id);
                    break;
                case PaymentType.Order:
                case PaymentType.Project:
                case PaymentType.Withdrawal:
                    try
                    {
                        await _balanceService.WithdrawAsync(payment.PayerId, amount, payment.Id);
                    }
                    catch (InvalidOperationException exception)
                    {
                        TempData["ErrorMessage"] = exception.Message;
                        return View("Details", payment);
                    }
                    break;
            }
            
            var balance = await _balanceService.GetAsync(payment.PayerId);
            TempData["NewBalance"] = balance.Balance.ToString("F2");

            if (payment.OrderId.HasValue)
            {
                var order = await _context.Orders.FindAsync(payment.OrderId);
                if (order != null && order.Status == OrderStatus.Accepted)
                {
                    order.Status = OrderStatus.Paid;
                }
            }

            if (payment.ProjectId.HasValue)
            {
                var project = await _context.Projects.FindAsync(payment.ProjectId);
                if (project != null && project.Status == ProjectStatus.InProgress)
                {
                    project.Status = ProjectStatus.Paid;
                }
            }
        }
        
        else if (status == ExternalPaymentsStatus.Canceled)
        {
            payment.Status = PaymentStatus.Canceled;
        }
        
        else if (status == ExternalPaymentsStatus.Failed)
        {
            payment.Status = PaymentStatus.Failed;
        }

        await _context.SaveChangesAsync();
        return View("Success", payment);
    }
    
    // cancel URL - покупатель вернулся с отменой
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Cancel(int? orderId, int? projectId)
    {
        Payment? payment = null;
        if (orderId.HasValue)
        {
            payment = await _context.Payments
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }
        
        else if (projectId.HasValue)
        {
            payment = await _context.Payments
                .Where(p => p.ProjectId == projectId)
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
        }

        else
        {
            return BadRequest("Не указан ни orderId, ни projectId");
        }

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
        var userRoles = await _userManager.GetRolesAsync(await _userManager.GetUserAsync(User));
        var isFreelancer = userRoles.Contains("Freelancer");

        IQueryable<Payment> query = _context.Payments
            .Include(p => p.Order)
            .ThenInclude(o => o!.Service);
            
        query = query.Include(p => p.Project);

        if (isFreelancer)
        {
            var payments = await query.ToListAsync();
            var items = payments.Where(p =>
                p.PayerId == userId ||
                (p.Type == PaymentType.Order && p.Order?.Service?.FreelancerId == userId) ||
                (p.Type == PaymentType.Project && p.Project?.SelectedFreelancerId == userId)
            ).OrderByDescending(p => p.CreatedAt).ToList();

            return View(items);
        }
        else
        {
            var items = await query
                .Where(p => p.PayerId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return View(items);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Deposit()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deposit(decimal amount)
    {
        if (amount <= 0)
        {
            ModelState.AddModelError("", "Сумма должна быть больше 0");
            return View();
        }
        
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.GetUserAsync(User);
        var balance = await _balanceService.GetAsync(userId); 
        
        long amountMinor = (long)(amount * 100m);

        var payment = new Payment
        {
            PayerId = user!.Id,
            AmountMinor = amountMinor,
            Currency = "RUB",
            Provider = "Stripe",
            Status = PaymentStatus.Pending,
            Type = PaymentType.Deposit
        };
        
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        
        var req = new CreateCheckoutSessionRequest
        {
            AmountMinor = amountMinor,
            Currency = "RUB",
            Description = $"Пополнение баланса на {amount:C} RUB",
            CustomerEmail = user.Email ?? "",
            SuccessUrl = Url.Action(nameof(Success), "Payment", null, Request.Scheme)!,
            CancelUrl = Url.Action(nameof(Deposit), "Payment", null, Request.Scheme)!,
            MetadataPaymentId = payment.Id.ToString()
        };

        var session = await _paymentProvider.CreateCheckoutSessionAsync(req);
        payment.ProviderSessionId = session.SessionId;
        payment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["NewBalance"] = balance.Balance.ToString("F2");
        return Redirect(session.SessionUrl);
    }
    
    [HttpGet]
    public async Task<IActionResult> Withdraw()
    {
        var userId = _userManager.GetUserId(User);
        var balance = await _balanceService.GetAsync(userId);
        return View(balance);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(decimal amount)
    {
        if (amount <= 0)
        {
            ModelState.AddModelError("", "Сумма должна быть больше 0");
            return View();
        }
        
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.GetUserAsync(User);
        var balance = await _balanceService.GetAsync(userId);

        if (balance.Balance < amount)
        {
            TempData["ErrorMessage"] = "Недостаточно средств";
            return RedirectToAction(nameof(Withdraw));
        }
        
        long amountMinor = (long)(amount * 100m);
        
        var payment = new Payment
        {
            PayerId = userId,
            AmountMinor = (long)(amount * 100m),
            Currency = "RUB",
            Provider = "Stripe",
            Status = PaymentStatus.Pending,
            Type = PaymentType.Withdrawal
        };
        
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        
        var req = new CreateCheckoutSessionRequest
        {
            AmountMinor = amountMinor,
            Currency = "RUB",
            Description = $"Вывод средств на сумму {amount:C} RUB",
            CustomerEmail = user.Email ?? "",
            SuccessUrl = Url.Action(nameof(Success), "Payment", null, Request.Scheme)!,
            CancelUrl = Url.Action(nameof(Deposit), "Payment", null, Request.Scheme)!,
            MetadataPaymentId = payment.Id.ToString()
        };
        
        var session = await _paymentProvider.CreateCheckoutSessionAsync(req);
        payment.ProviderSessionId = session.SessionId;
        payment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        TempData["NewBalance"] = balance.Balance.ToString("F2");
        return Redirect(session.SessionUrl);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyBalance()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new { balance = 0 });
        }

        var balance = await _balanceService.GetAsync(user.Id);
        return Json(new { balance });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var payment = await _context.Payments
            .Include(o => o.Order)
            .ThenInclude(o => o!.Service)
            .Include(p => p.Project)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }
        
        var userId = _userManager.GetUserId(User);
        if (payment.PayerId != userId)
        {
            return Forbid();
        }

        return View("Success", payment);
    }
}