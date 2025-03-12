using Microsoft.EntityFrameworkCore;
using RapidPay.FeeManagement.Domain.Entities;

namespace RapidPay.FeeManagement.Infrastructure.Persistence;

public class FeeDbContext(DbContextOptions<FeeDbContext> options)
    : DbContext(options)
{
    public DbSet<Fee> Fees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Fee>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Fee>()
            .Property(x => x.Value)
            .HasPrecision(18, 4);
    }
}