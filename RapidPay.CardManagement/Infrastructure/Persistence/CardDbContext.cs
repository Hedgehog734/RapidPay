using Microsoft.EntityFrameworkCore;
using RapidPay.CardManagement.Domain.Entities;

namespace RapidPay.CardManagement.Infrastructure.Persistence;

public class CardDbContext(DbContextOptions<CardDbContext> options)
    : DbContext(options)
{
    public DbSet<Card> Cards { get; set; }
    public DbSet<CardTransactionLog> CardsLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Card>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<Card>()
            .HasIndex(x => x.CardNumber)
            .IsUnique();

        modelBuilder.Entity<Card>()
            .Property(x => x.CardNumber)
            .HasMaxLength(15)
            .IsRequired();

        modelBuilder.Entity<Card>()
            .Property(x => x.CreditLimit)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Card>()
            .Property(x => x.UsedCredit)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Card>()
            .Property(x => x.Balance)
            .HasPrecision(18, 4);

        modelBuilder.Entity<CardTransactionLog>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<CardTransactionLog>()
            .Property(x => x.TransactionType)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<CardTransactionLog>()
            .Property(x => x.Amount)
            .HasPrecision(18, 4);
    }
}