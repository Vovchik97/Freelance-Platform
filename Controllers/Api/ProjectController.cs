using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Api;

[Route("api/[controller]")]
[ApiController]
public class ProjectController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProjectController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Project>>> ListProjects([FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] decimal? minBudget,
        [FromQuery] decimal? maxBudget,
        [FromQuery] string? sort)
    {
        var query = _context.Projects.Include(p => p.Client).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Title.Contains(search) || p.Description.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ProjectStatus>(status, out var parsedStatus))
        {
            query = query.Where(p => p.Status == parsedStatus);
        }

        if (minBudget.HasValue)
        {
            query = query.Where(p => p.Budget >= minBudget);
        }

        if (maxBudget.HasValue)
        {
            query = query.Where(p => p.Budget <= maxBudget);
        }

        // Сортировка
        if (sort == "budget_desc")
            query = query.OrderByDescending(p => p.Budget);
        else if (sort == "budget_asc")
            query = query.OrderBy(p => p.Budget);
        else
            query = query.OrderByDescending(p => p.CreatedAt);
        
        return await query.Include(p => p.Client).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Project>> GetProject(int id)
    {
        var project = await _context.Projects.Include(p => p.Client).FirstOrDefaultAsync(p => p.Id == id);
        if (project == null)
        {
            return BadRequest();
        }

        return project;
    }
    
    [HttpPost]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Client")]
    public async Task<ActionResult<Project>> CreateProject([FromBody] CreateProjectDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                     throw new InvalidOperationException("user is not authenticated");

        var project = new Project
        {
            Title = dto.Title,
            Description = dto.Description,
            Budget = dto.Budget,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow,
            ClientId = userId
        };
        
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        
        await _context.Entry(project).Reference(p => p.Client).LoadAsync();

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    [HttpPut("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Client")]
    public async Task<ActionResult<Project>> UpdateProject(int id,  UpdateProjectDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects.Include(p => p.Client).FirstOrDefaultAsync(p => p.Id == id);
        
        if (project == null)
        {
            return NotFound();
        }
        
        if (project.ClientId != userId)
        {
            return Forbid();
        }

        project.Title = dto.Title;
        project.Description = dto.Description;
        project.Budget = dto.Budget;
        project.Status = dto.Status;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Projects.Any(p => p.Id == id))
            {
                return NotFound();
            }
            throw;
        }
        
        return project;
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Client")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects.FindAsync(id);
        
        if (project == null)
        {
            return NotFound();
        }
        
        if (project.ClientId != userId)
        {
            return Forbid();
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateProjectDto
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Budget { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Open;
}

public class UpdateProjectDto
{
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Budget { get; set; }
    public ProjectStatus Status { get; set; }
}