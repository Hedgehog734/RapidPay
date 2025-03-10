using Microsoft.EntityFrameworkCore;
using RapidPay.Transaction.Domain.Entities;

namespace RapidPay.Transaction.Infrastructure.Persistent;

public class TransactionDbContext(DbContextOptions<TransactionDbContext> options)
    : DbContext(options)
{
    public DbSet<CardTransaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CardTransaction>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<CardTransaction>()
            .Property(x => x.Amount)
            .HasPrecision(18, 4);
    }
}