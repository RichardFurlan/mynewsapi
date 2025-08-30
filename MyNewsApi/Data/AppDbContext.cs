using Microsoft.EntityFrameworkCore;
using MyNewsApi.Models;

namespace MyNewsApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {}
    
    public DbSet<Article> Articles { get; set; }
}