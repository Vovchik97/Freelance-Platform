using FreelancePlatform.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace FreelancePlatform.Context;

public class AppDbContext : IdentityDbContext
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<Bid> Bids { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<UserBalance> UserBalances { get; set; }
    public DbSet<BalanceTransaction> BalanceTransactions { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<ProjectTask> ProjectTasks { get; set; }
    public DbSet<ProjectActivityLog> ProjectActivityLogs { get; set; }
    public DbSet<GroupChatMessage> GroupChatMessages { get; set; }
    public DbSet<GroupChatMention> GroupChatMentions { get; set; }
    public DbSet<GroupChatMessageRead> GroupChatMessageReads { get; set; }
    public DbSet<PaymentShare> PaymentShares { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) 
    {
        
    }
    
}