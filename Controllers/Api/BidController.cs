using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Dto.Bids;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class BidController : ControllerBase
{
    private readonly AppDbContext _context;

    public BidController(AppDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Bid>>> ListBids([FromQuery] int? projectId, [FromQuery] string? freelancerId)
    {
        var query = _context.Bids.AsQueryable();

        if (projectId != null)
        {
            query = query.Where(b => b.ProjectId == projectId);
        }

        if (!string.IsNullOrEmpty(freelancerId))
        {
            query = query.Where(b => b.FreelancerId == freelancerId);
        }
        
        return await query
            .Include(b => b.Project.Client)
            .Include(b => b.Freelancer)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Bid>> GetBid(int id)
    {
        var bid = await _context.Bids
            .Include(b => b.Project.Client)
            .Include(b => b.Freelancer)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (bid == null)
        {
            return BadRequest();
        }

        return bid;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Freelancer")]
    public async Task<ActionResult<Bid>> CreateBid([FromBody] CreateBidDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     throw new InvalidOperationException("user is not authenticated");
        
        var project = await _context.Projects
            .Include(p => p.Client)
            .FirstOrDefaultAsync(p => p.Id == dto.ProjectId);

        if (project == null)
        {
            return NotFound();
        }
        
        var duplicate = await _context.Bids
            .AnyAsync(b => b.ProjectId == dto.ProjectId && b.FreelancerId == userId);

        if (duplicate)
        {
            return Conflict("You have already applied for this project.");
        }
        
        var bid = new Bid
        {
            ProjectId = dto.ProjectId,
            Amount = dto.Amount,
            Comment = dto.Comment,
            DurationInDays = dto.DurationInDays,
            CreatedAt = DateTime.UtcNow,
            FreelancerId = userId
        };

        _context.Bids.Add(bid);
        await _context.SaveChangesAsync();
        
        await _context.Entry(bid).Reference(b => b.Project).LoadAsync();
        if (bid.Project != null)
        {
            await _context.Entry(bid.Project).Reference(p => p.Client).LoadAsync();
        }
        await _context.Entry(bid).Reference(b => b.Freelancer).LoadAsync();
        
        return CreatedAtAction(nameof(GetBid), new {id = bid.Id}, bid);
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Freelancer")]
    public async Task<ActionResult<Bid>> UpdateBid(int id, [FromBody] UpdateBidDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bid = await _context.Bids
            .Include(b => b.Project.Client)
            .Include(b => b.Freelancer)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (bid == null)
        {
            return NotFound();
        }

        if (bid.FreelancerId != userId)
        {
            return Forbid();
        }

        bid.Amount = dto.Amount;
        bid.Comment = dto.Comment;
        bid.DurationInDays = dto.DurationInDays;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Bids.Any(b => b.Id == id))
            {
                return NotFound();
            }
            throw;
        }

        return bid;
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Freelancer")]
    public async Task<ActionResult<Bid>> DeleteBid(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var bid = await _context.Bids.FindAsync(id);

        if (bid == null)
        {
            return NotFound();
        }

        if (bid.FreelancerId != userId)
        {
            return Forbid();
        }

        _context.Bids.Remove(bid);
        await _context.SaveChangesAsync();
        
        return NoContent();
    }
}