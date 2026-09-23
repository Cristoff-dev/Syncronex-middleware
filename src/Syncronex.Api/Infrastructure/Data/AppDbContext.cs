using Microsoft.EntityFrameworkCore;

namespace Syncronex.Api.Infrastructure.Data;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  public DbSet<DeadLetterRecord> DeadLetters { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Add an index to speed up searches by the original webhook ID
    modelBuilder.Entity<DeadLetterRecord>()
        .HasIndex(d => d.SourceEventId)
        .IsUnique(false);
  }
}
