    using System.Security.Claims;
    using FreelancePlatform.Context;
    using FreelancePlatform.Dto.Bids;
    using FreelancePlatform.Models;
    using FreelancePlatform.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;

    namespace FreelancePlatform.Controllers.Web;

    public class BidController : Controller
    {
        private readonly AppDbContext _context;
        private readonly SmtpEmailSender _emailSender;

        public BidController(AppDbContext context, SmtpEmailSender emailSender)
        {
            _context = context;
            _emailSender = emailSender;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var bid = await _context.Bids
                .Include(b => b.Freelancer)
                .Include(b => b.Project)
                    .ThenInclude(p => p!.Client)
                .FirstOrDefaultAsync(b => b.Id == id);
            

            if (bid == null)
            {
                return NotFound();
            }

            return View(bid);
        }

        [Authorize(Roles = "Freelancer")]
        public IActionResult Create(int projectId)
        {
            var dto = new CreateBidDto
            {
                ProjectId = projectId
            };
            ViewBag.ProjectId = projectId;
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Freelancer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBidDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var alreadyExists = await _context.Bids.AnyAsync(b => b.ProjectId == dto.ProjectId && b.FreelancerId == userId);

            if (alreadyExists)
            {
                ModelState.AddModelError(string.Empty, "Вы уже отправили заявку на этот проект.");
                ViewBag.ProjectId = dto.ProjectId;
                return View(dto);
            }
            
            var bid = new Bid
            {
                Amount = dto.Amount,
                Comment = dto.Comment,
                DurationInDays = dto.DurationInDays,
                CreatedAt = DateTime.UtcNow,
                FreelancerId = userId,
                ProjectId = dto.ProjectId
            };

            await _context.Bids.AddAsync(bid);
            await _context.SaveChangesAsync();
            
            await _context.Entry(bid).Reference(b => b.Project).LoadAsync();
            if (bid.Project != null)
            {
                await _context.Entry(bid.Project).Reference(p => p.Client).LoadAsync();
            }

            await _context.Entry(bid).Reference(b => b.Freelancer).LoadAsync();

            var clientEmail = bid.Project?.Client?.Email;

            if (!string.IsNullOrWhiteSpace(clientEmail))
            {
                await _emailSender.SendEmailAsync(
                    toEmail: clientEmail,
                    subject: "Новая заявка на проект",
                    bodyHtml: $"Пользователь {bid.Freelancer?.UserName} оставил заявку на проект {bid.Project?.Title ?? "Без названия"}."
                );
            }

            
            return RedirectToAction(nameof(MyBids));
        }

        [Authorize(Roles = "Freelancer")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bid = await _context.Bids.FirstOrDefaultAsync(b => b.Id == id && b.FreelancerId == userId);

            if (bid == null)
            {
                return NotFound();
            }

            var dto = new UpdateBidDto
            {
                Amount = bid.Amount,
                Comment = bid.Comment,
                DurationInDays = bid.DurationInDays
            };

            return View(dto);
        }

        [HttpPost]
        [Authorize(Roles = "Freelancer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UpdateBidDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bid = await _context.Bids.FirstOrDefaultAsync(b => b.Id == id && b.FreelancerId == userId);

            if (bid == null)
            {
                return NotFound();
            }

            bid.Amount = dto.Amount;
            bid.Comment = dto.Comment;
            bid.DurationInDays = dto.DurationInDays;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyBids));
        }

        [HttpPost]
        [Authorize(Roles = "Freelancer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var bid = await _context.Bids.FirstOrDefaultAsync(b => b.Id == id && b.FreelancerId == userId);

            if (bid == null)
            {
                return NotFound();
            }

            _context.Bids.Remove(bid);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyBids));
        }

        [Authorize(Roles = "Freelancer")]
        public async Task<IActionResult> MyBids()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var myBids = await _context.Bids
                .Include(b => b.Project)
                .Where(b => b.FreelancerId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(myBids);
        }
    }