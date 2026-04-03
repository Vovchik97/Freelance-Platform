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
        var freelancerServices = await _context.Services
            .Where(s => s.FreelancerId == freelancerId)
            .Include(s => s.Categories)
            .ToListAsync();
        
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

        if (!candidateProjects.Any())
        {
            return new List<RecommendationDto>();
        }

        var serviceCategoryIds = freelancerServices
            .SelectMany(s => s.Categories)
            .Select(c => c.Id)
            .Distinct()
            .ToHashSet();

        var bidCategoryIds = biddedProjects
            .SelectMany(p => p.Categories)
            .Select(c => c.Id)
            .Distinct()
            .ToHashSet();

        var allCategoryIds = new HashSet<int>(serviceCategoryIds);
        allCategoryIds.UnionWith(bidCategoryIds);

        var prices = new List<decimal>();
        prices.AddRange(freelancerServices.Select(s => s.Price));
        prices.AddRange(biddedProjects.Select(p => p.Budget));
        
        decimal? targetBudget = prices.Any() ? prices.Average() : null;

        if (!allCategoryIds.Any() && targetBudget == null)
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
                    Categories = p.Categories.Select(c => c.Name).ToList(),
                    Type = "Project",
                    Reasons = new List<string> { "Новый проект" }
                })
                .ToList();
        }
        
        var scored = new List<RecommendationDto>();

        foreach (var project in candidateProjects)
        {
            double score = 0;
            var reasons = new List<string>();

            var projectCategoryIds = project.Categories.Select(c => c.Id).ToHashSet();

            var serviceMatch = projectCategoryIds
                .Intersect(serviceCategoryIds).Count();

            if (serviceMatch > 0)
            {
                score += 0.4 * Math.Min(serviceMatch, 3);
                reasons.Add("Совпадает с вашими услугами");
            }

            var bidMatch = projectCategoryIds
                .Intersect(bidCategoryIds)
                .Except(serviceCategoryIds)
                .Count();
            
            if (bidMatch > 0)
            {
                score += 0.2 * Math.Min(bidMatch, 3);
                reasons.Add("Похож на проекты из откликов");
            }

            if (targetBudget.HasValue && targetBudget.Value > 0)
            {
                var budgetDiff = Math.Abs(targetBudget.Value - project.Budget);
                if (budgetDiff < targetBudget.Value * 0.3m)
                {
                    score += 0.2;
                    reasons.Add("Подходящий бюджет");
                }
                else if (budgetDiff < targetBudget.Value * 0.6m)
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
                Categories = project.Categories.Select(c => c.Name).ToList(),
                Type = "Project",
                Reasons = reasons
            });
        }

        return scored.OrderByDescending(r => r.Score).Take(6).ToList();
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

        var projectCategoryIds = clientProjects
            .SelectMany(p => p.Categories)
            .Select(c => c.Id)
            .Distinct()
            .ToHashSet();

        var orderCategoryIds = clientOrders
            .Where(o => o.Service != null)
            .SelectMany(o => o.Service!.Categories)
            .Select(c => c.Id)
            .Distinct()
            .ToHashSet();

        var allCategoryIds = new HashSet<int>(projectCategoryIds);
        allCategoryIds.UnionWith(orderCategoryIds);

        var prices = new List<decimal>();
        prices.AddRange(clientProjects.Select(p => p.Budget));
        prices.AddRange(clientOrders
            .Where(o => o.Service != null)
            .Select(o => o.Service!.Price));

        decimal? targetPrice = prices.Any() ? prices.Average() : null;

        if (!allCategoryIds.Any() && targetPrice == null)
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
                    Categories = s.Categories.Select(c => c.Name).ToList(),
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

            var serviceCatIds = service.Categories
                .Select(c => c.Id)
                .ToHashSet();

            var projectMatch = serviceCatIds
                .Intersect(projectCategoryIds).Count();

            if (projectMatch > 0)
            {
                score += 0.4 * Math.Min(projectMatch, 3);
                reasons.Add("Подходит под ваши проекты");
            }

            var orderMatch = serviceCatIds
                .Intersect(orderCategoryIds)
                .Except(projectCategoryIds)
                .Count();

            if (orderMatch > 0)
            {
                score += 0.2 * Math.Min(orderMatch, 3);
                reasons.Add("Похожа на прошлые заказы");
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
                Categories = service.Categories.Select(c => c.Name).ToList(),
                Type = "Service",
                Reasons = reasons
            });
        }

        return scored.OrderByDescending(r => r.Score).Take(6).ToList();
    }
}