using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Dto.Projects;
using FreelancePlatform.Dto.Services;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class ServiceController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServiceController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Service>>> ListServices([FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] decimal? minBudget,
        [FromQuery] decimal? maxBudget,
        [FromQuery] string? sort)
    {
        var query = _context.Services.Include(p => p.SelectedClient).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Title.Contains(search) || s.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ServiceStatus>(status, out var parsedStatus))
        {
            query = query.Where(s => s.Status == parsedStatus);
        }

        if (minBudget.HasValue)
        {
            query = query.Where(s => s.Price >= minBudget);
        }

        if (maxBudget.HasValue)
        {
            query = query.Where(s => s.Price <= maxBudget);
        }

        // Сортировка
        if (sort == "budget_desc")
            query = query.OrderByDescending(s => s.Price);
        else if (sort == "budget_asc")
            query = query.OrderBy(s => s.Price);
        else
            query = query.OrderByDescending(s => s.CreatedAt);
        
        return await query.Include(s => s.SelectedClient).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Service>> GetService(int id)
    {
        var service = await _context.Services.Include(s => s.SelectedClient).FirstOrDefaultAsync(s => s.Id == id);
        if (service == null)
        {
            return BadRequest();
        }

        return service;
    }
    
    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Freelancer")]
    public async Task<ActionResult<Service>> CreateService([FromBody] CreateServiceDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                     throw new InvalidOperationException("user is not authenticated");

        var service = new Service
        {
            Title = dto.Title,
            Description = dto.Description,
            Price = dto.Price,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow,
            FreelancerId = userId
        };
        
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        
        await _context.Entry(service).Reference(s => s.Freelancer).LoadAsync();

        return CreatedAtAction(nameof(GetService), new { id = service.Id }, service);
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Freelancer")]
    public async Task<ActionResult<Service>> UpdateService(int id,  UpdateServiceDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var service = await _context.Services.Include(s => s.Freelancer).FirstOrDefaultAsync(s => s.Id == id);
        
        if (service == null)
        {
            return NotFound();
        }
        
        if (service.FreelancerId != userId)
        {
            return Forbid();
        }

        service.Title = dto.Title;
        service.Description = dto.Description;
        service.Price = dto.Price;
        service.Status = dto.Status;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Services.Any(s => s.Id == id))
            {
                return NotFound();
            }
            throw;
        }
        
        return service;
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Freelancer")]
    public async Task<IActionResult> DeleteService(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var service = await _context.Services.FindAsync(id);
        
        if (service == null)
        {
            return NotFound();
        }
        
        if (service.FreelancerId != userId)
        {
            return Forbid();
        }

        _context.Services.Remove(service);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}