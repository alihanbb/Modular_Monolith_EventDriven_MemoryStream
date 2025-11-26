using LoyaltyService.Loyalty;
using Microsoft.EntityFrameworkCore;

namespace Loyalty;

public class LoyaltyDbContext : DbContext
{
    public LoyaltyDbContext(DbContextOptions<LoyaltyDbContext> options)
        : base(options)
    {
    }

    public DbSet<UserPoints> UserPoints { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserPoints>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.TotalPoints).HasDefaultValue(0);
            entity.Property(e => e.AvailablePoints).HasDefaultValue(0);
            entity.Property(e => e.UsedPoints).HasDefaultValue(0);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });
    }
}
