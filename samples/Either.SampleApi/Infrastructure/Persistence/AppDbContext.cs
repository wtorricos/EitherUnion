using Either.SampleApi.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Either.SampleApi.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<RefundEntity> Refunds => Set<RefundEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<OrderEntity>(builder =>
        {
            _ = builder.HasKey(order => order.Id);
            _ = builder.Property(order => order.CustomerName).IsRequired();
            _ = builder.Property(order => order.Currency).IsRequired();
        });

        _ = modelBuilder.Entity<RefundEntity>(builder =>
        {
            _ = builder.HasKey(refund => refund.Id);
            _ = builder.Property(refund => refund.Reason).IsRequired();
        });
    }
}
