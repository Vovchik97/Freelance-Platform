using FreelancePlatform.Context;
using FreelancePlatform.Models;

namespace FreelancePlatform.Services;

public class ProjectActivityLogService
{
    private readonly AppDbContext _context;

    public ProjectActivityLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int projectId, string action, string? actorId = null, string? actorName = null)
    {
        var log = new ProjectActivityLog
        {
            ProjectId = projectId,
            Action = action,
            ActorId = actorId,
            ActorName = actorName,
            CreatedAt = DateTime.UtcNow
        };

        _context.ProjectActivityLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}