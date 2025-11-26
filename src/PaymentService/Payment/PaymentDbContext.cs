using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace PaymentService.Payment
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        public DbSet<Payment> Payments { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.AddInboxStateEntity();
            modelBuilder.AddOutboxMessageEntity();
            modelBuilder.AddOutboxStateEntity();

            modelBuilder.Entity<Payment>(entity =>
            {
                entity.HasKey(e => e.Id);

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

        }
    }
}
