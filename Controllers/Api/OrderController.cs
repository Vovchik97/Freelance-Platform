using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Dto.Bids;
using FreelancePlatform.Dto.Orders;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class OrderController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrderController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> ListOrders([FromQuery] int? serviceId, [FromQuery] string? clientId)
    {
        var query = _context.Orders.AsQueryable();

        if (serviceId != null)
        {
            query = query.Where(o => o.ServiceId == serviceId);
        }

        if (!string.IsNullOrEmpty(clientId))
        {
            query = query.Where(o => o.ClientId == clientId);
        }
        
        return await query
            .Include(o => o.Service)
                .ThenInclude(s => s!.Freelancer)
            .Include(o => o.Client)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Service)
                .ThenInclude(s => s!.Freelancer)
            .Include(o => o.Client)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order == null)
        {
            return BadRequest();
        }

        return order;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Client")]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     throw new InvalidOperationException("user is not authenticated");
        
        var service = await _context.Services
            .Include(s => s.Freelancer)
            .FirstOrDefaultAsync(s => s.Id == dto.ServiceId);

        if (service == null)
        {
            return NotFound();
        }
        
        var duplicate = await _context.Orders
            .AnyAsync(o => o.ServiceId == dto.ServiceId && o.ClientId == userId);

        if (duplicate)
        {
            return Conflict("You have already applied for this project.");
        }
        
        var order = new Order
        {
            ServiceId = dto.ServiceId,
            Comment = dto.Comment,
            DurationInDays = dto.DurationInDays,
            CreatedAt = DateTime.UtcNow,
            ClientId = userId
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        
        await _context.Entry(order).Reference(o => o.Service).LoadAsync();
        if (order.Service != null)
        {
            await _context.Entry(order.Service).Reference(o => o.Freelancer).LoadAsync();
        }
        await _context.Entry(order).Reference(o => o.Client).LoadAsync();
        
        return CreatedAtAction(nameof(GetOrder), new {id = order.Id}, order);
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Client")]
    public async Task<ActionResult<Order>> UpdateBid(int id, [FromBody] UpdateOrderDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _context.Orders
            .Include(o => o.Service)
                .ThenInclude(s => s!.Freelancer)
            .Include(o => o.Client)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        if (order.ClientId != userId)
        {
            return Forbid();
        }

        order.Comment = dto.Comment;
        order.DurationInDays = dto.DurationInDays;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Orders.Any(b => b.Id == id))
            {
                return NotFound();
            }
            throw;
        }

        return order;
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Client")]
    public async Task<ActionResult<Order>> DeleteOrder(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var order = await _context.Orders.FindAsync(id);

        if (order == null)
        {
            return NotFound();
        }

        if (order.ClientId != userId)
        {
            return Forbid();
        }

        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}