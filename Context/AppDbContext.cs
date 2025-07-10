using FreelancePlatform.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FreelancePlatform.Context;

public class AppDbContext : IdentityDbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Bid> Bids { get; set; }
    public DbSet<Message> Messages { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
    {
        
    }
}