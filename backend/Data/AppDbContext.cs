using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<TimelineEntry> TimelineEntries { get; set; } = null!;
    public DbSet<ApiConnection> ApiConnections { get; set; }
}
