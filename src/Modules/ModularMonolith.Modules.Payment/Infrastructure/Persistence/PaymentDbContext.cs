
using Microsoft.EntityFrameworkCore;
using ModularMonolith.Shared.EventBus;

namespace ModularMonolith.Modules.Payment.Infrastructure.Persistence;

internal class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

    public DbSet<Domain.Entities.Payment> Payments { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("payment");

        modelBuilder.Entity<Domain.Entities.Payment>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId)
                  .IsRequired()
                  .HasMaxLength(450);

            entity.Property(e => e.Amount)
                  .HasPrecision(18, 2)
                  .IsRequired();

            entity.Property(e => e.PaymentMethod)
                  .HasMaxLength(50)
                  .IsRequired()
                  .HasDefaultValue("credit card");

            entity.Property(e => e.Status)
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.Property(e => e.ProcessedAt)
                  .HasDefaultValueSql("NOW()");

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("outbox_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.ProcessedAt);
        });
    }
}
