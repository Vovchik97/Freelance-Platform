using FreelancePlatform.Context;
using FreelancePlatform.Dto.Recommendations;
using FreelancePlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace FreelancePlatform.Services;

public class RecommendationService
{
    private readonly AppDbContext _context;
    
    public RecommendationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecommendationDto>> GetRecommendedProjectsForFreelancerAsync(string freelancerId)
    {
        var userBids = await _context.Bids
            .Where(b => b.FreelancerId == freelancerId)
            .Include(b => b.Project)
                .ThenInclude(p => p.Categories)
            .ToListAsync();
        
        var biddedProjects = userBids
            .Where(b => b.Project != null)
            .Select(b => b.Project!)
            .ToList();

        var biddedProjectIds = biddedProjects
            .Select(p => p.Id)
            .ToHashSet();

        var candidateProjects = await _context.Projects
            .Where(p => p.Status == ProjectStatus.Open)
            .Where(p => !biddedProjectIds.Contains(p.Id))
            .Include(p => p.Categories)
            .ToListAsync();

        if (!biddedProjects.Any())
        {
            return candidateProjects
                .OrderByDescending(p => p.CreatedAt)
                .Take(6)
                .Select(p => new RecommendationDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Budget = p.Budget,
                    Score = 0,
                    Type = "Project",
                    Reasons = new List<string> { "Новый проект" }
                })
                .ToList();
        }

        var avgBudget = biddedProjects.Average(p => p.Budget);

        var userCategoryIds = biddedProjects.SelectMany(p => p.Categories)
            .Select(c => c.Id)
            .Distinct()
            .ToHashSet();
        
        var scored = new List<RecommendationDto>();

        foreach (var project in candidateProjects)
        {
            double score = 0;
            var reasons = new List<string>();

            var projectCategoryIds = project.Categories.Select(c => c.Id).ToHashSet();
            var commonCategories = projectCategoryIds.Intersect(userCategoryIds).Count();
            if (commonCategories > 0)
            {
                score += 0.3 * Math.Min(commonCategories, 3);
                reasons.Add("Похожая категория");
            }

            if (avgBudget > 0)
            {
                var budgetDiff = Math.Abs(project.Budget - avgBudget);
                if (budgetDiff < avgBudget * 0.3m)
                {
                    score += 0.2;
                    reasons.Add("Похожий бюджет");
                }
                else if (budgetDiff < avgBudget * 0.6m)
                {
                    score += 0.1;
                    reasons.Add("Приемлемый бюджет");
                }
            }
            
            var daysOld = (DateTime.UtcNow - project.CreatedAt).TotalDays;
            if (daysOld < 1)
            {
                score += 0.15;
                reasons.Add("только что опубликован");
            }
            else if (daysOld < 3)
            {
                score += 0.1;
                reasons.Add("новый проект");
            }

            if (reasons.Count == 0)
            {
                reasons.Add("Может быть интересен");
            }
            
            scored.Add(new RecommendationDto
            {
                Id = project.Id,
                Title = project.Title,
                Budget = project.Budget,
                Score = score,
                Type = "Project",
                Reasons = reasons
            });
        }

        return scored.OrderByDescending(r => r.Score).Take(5).ToList();
    }

    public async Task<List<RecommendationDto>> GetRecommendedServicesForClientAsync(string clientId)
    {
        var clientProjects = await _context.Projects
            .Where(p => p.ClientId == clientId)
            .Include(p => p.Categories)
            .ToListAsync();

        var clientOrders = await _context.Orders
            .Where(o => o.ClientId == clientId)
            .Include(o => o.Service)
                .ThenInclude(s => s.Categories)
            .ToListAsync();
        
        var orderedServiceIds = clientOrders
            .Where(o => o.Service != null)
            .Select(o => o.Service!.Id)
            .ToHashSet();

        var candidateServices = await _context.Services
            .Where(s => s.Status == ServiceStatus.Available)
            .Where(s => s.FreelancerId != clientId)
            .Where(s => !orderedServiceIds.Contains(s.Id))
            .Include(s => s.Categories)
            .Include(s => s.Reviews)
            .Include(s => s.Freelancer)
            .ToListAsync();

        if (!candidateServices.Any())
        {
            return new List<RecommendationDto>();
        }

        var categoryIds = new HashSet<int>();

        foreach (var p in clientProjects)
        {
            foreach (var c in p.Categories)
            {
                categoryIds.Add(c.Id);
            }
        }

        foreach (var o in clientOrders.Where(o => o.Service != null))
        {
            foreach (var c in o.Service!.Categories)
            {
                categoryIds.Add(c.Id);
            }
        }

        decimal? avgBudget = clientProjects.Any()
            ? clientProjects.Average(p => p.Budget)
            : null;

        decimal? avgOrderPrice = clientOrders.Any(o => o.Service != null)
            ? clientOrders.Where(o => o.Service != null).Average(o => o.Service!.Price)
            : null;

        decimal? targetPrice = (avgBudget, avgOrderPrice) switch
        {
            (not null, not null) => (avgBudget.Value + avgOrderPrice.Value) / 2,
            (not null, null) => avgBudget.Value,
            (null, not null) => avgOrderPrice.Value,
            _ => null
        };

        if (!categoryIds.Any() && targetPrice == null)
        {
            return candidateServices
                .OrderByDescending(s =>
                    (s.Reviews?.Any() == true ? s.Reviews.Average(r => r.Rating) : 0) * 0.7
                    + (1.0 / ((DateTime.UtcNow - s.CreatedAt).TotalDays + 1)) * 0.3
                )
                .Take(6)
                .Select(s => new RecommendationDto
                {
                    Id = s.Id,
                    Title = s.Title,
                    Budget = s.Price,
                    Score = 0,
                    Type = "Service",
                    Reasons = new List<string> { "Популярная услуга" }
                })
                .ToList();
        }

        var scored = new List<RecommendationDto>();

        foreach (var service in candidateServices)
        {
            double score = 0;
            var reasons = new List<string>();

            var serviceCategoryIds = service.Categories.Select(c => c.Id).ToHashSet();
            var commonCount = serviceCategoryIds.Intersect(categoryIds).Count();
            if (commonCount > 0)
            {
                score += 0.3 * Math.Min(commonCount, 3);
                reasons.Add("Подходящая категория");
            }

            if (targetPrice.HasValue && targetPrice.Value > 0)
            {
                var priceDiff = Math.Abs(service.Price - targetPrice.Value);
                if (priceDiff < targetPrice.Value * 0.3m)
                {
                    score += 0.2;
                    reasons.Add("Подходяшая цена");
                }
                else if (priceDiff < targetPrice.Value * 0.6m)
                {
                    score += 0.1;
                    reasons.Add("Приемлемая цена");
                }
            }
            
            if (service.Reviews?.Any() == true)
            {
                var avgRating = service.Reviews.Average(r => r.Rating);
                if (avgRating >= 4.5)
                {
                    score += 0.2;
                    reasons.Add($"Высокий рейтинг ({avgRating:F1})");
                }
                else if (avgRating >= 3.5)
                {
                    score += 0.1;
                    reasons.Add($"Хороший рейтинг ({avgRating:F1})");
                }
            }

            var daysOld = (DateTime.UtcNow - service.CreatedAt).TotalDays;
            if (daysOld < 1)
            {
                score += 0.1;
                reasons.Add("Новая услуга");
            }
            else if (daysOld < 7)
            {
                score += 0.05;
                reasons.Add("Недавно добавлена");
            }

            if (reasons.Count == 0)
            {
                reasons.Add("Может быть полезна");
            }
            
            scored.Add(new RecommendationDto
            {
                Id = service.Id,
                Title = service.Title,
                Budget = service.Price,
                Score = score,
                Type = "Service",
                Reasons = reasons
            });
        }

        return scored.OrderByDescending(r => r.Score).Take(5).ToList();
    }
}