using Microsoft.EntityFrameworkCore;
using MyNewsApi.Models;

namespace MyNewsApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {}
    
    public DbSet<News> News { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<News>()
            .HasIndex(a => a.Url)
            .IsUnique(false);
        
        modelBuilder.Entity<News>()
            .HasOne(a => a.User)
            .WithMany(u => u.News)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        base.OnModelCreating(modelBuilder);
    }
}