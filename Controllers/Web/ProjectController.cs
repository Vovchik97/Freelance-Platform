﻿using System.Security.Claims;
using FreelancePlatform.Context;
using FreelancePlatform.Controllers.Api;
using FreelancePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Controllers.Web;

public class ProjectController : Controller
{
    private readonly AppDbContext _context;

    public ProjectController(AppDbContext context)
    {
        _context = context;
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? search, string? status, decimal? minBudget, decimal? maxBudget, string sort)
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
        
        if (sort == "budget_desc")
            query = query.OrderByDescending(p => p.Budget);
        else if (sort == "budget_asc")
            query = query.OrderBy(p => p.Budget);
        else
            query = query.OrderByDescending(p => p.CreatedAt);
        
        var projects = await query.ToListAsync();

        ViewBag.Searcg = search;
        ViewBag.Status = status;
        ViewBag.MinBudget = minBudget;
        ViewBag.MaxBudget = maxBudget;
        ViewBag.Sort = sort;
        
        return View(projects);
    }
    
    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var project = await _context.Projects
            .Include(p => p.Client)
            .Include(p => p.Bids)
                .ThenInclude(b => b.Freelancer)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (project == null)
        {
            return NotFound();
        }
        
        return View(project);
    }

    [Authorize(Roles = "Client")]
    public IActionResult Create()
    {
        return View();
    }
    
    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateProjectDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        var project = new Project
        {
            Title = dto.Title,
            Description = dto.Description,
            Budget = dto.Budget,
            Status = dto.Status,
            ClientId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            CreatedAt = DateTime.UtcNow
        };
        
        await _context.Projects.AddAsync(project);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(MyProjects));
    }
    
    [Authorize(Roles = "Client")]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);
        
        if (project == null)
        {
            return NotFound();
        }

        var dto = new UpdateProjectDto
        {
            Title = project.Title,
            Description = project.Description,
            Budget = project.Budget,
            Status = project.Status
        };
        
        return View(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateProjectDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }
        
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }
        
        project.Title = dto.Title;
        project.Description = dto.Description;
        project.Budget = dto.Budget;
        project.Status = dto.Status;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(MyProjects));
    }

    [HttpPost]
    [Authorize(Roles = "Client")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == id && p.ClientId == userId);

        if (project == null)
        {
            return NotFound();
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(MyProjects));
    }

    [Authorize(Roles = "Client")]
    public async Task<IActionResult> MyProjects()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var myProjects = await _context.Projects
            .Where(p => p.ClientId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(myProjects);
    }
}