using Microsoft.EntityFrameworkCore;
using RapidPay.Authorization.Domain.Entities;

namespace RapidPay.Authorization.Infrastructure.Persistence;

public class AuthorizationDbContext(DbContextOptions<AuthorizationDbContext> options)
    : DbContext(options)
{
    public DbSet<CardAuthorization> CardAuthorizations { get; set; }
    public DbSet<AuthorizationLog> AuthorizationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CardAuthorization>()
            .HasKey(x => x.CardNumber);

        modelBuilder.Entity<CardAuthorization>()
            .Property(x => x.CardNumber)
            .HasMaxLength(15)
            .IsRequired();

        modelBuilder.Entity<AuthorizationLog>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<AuthorizationLog>()
            .Property(x => x.CardNumber)
            .HasMaxLength(15)
            .IsRequired();

        modelBuilder.Entity<AuthorizationLog>()
            .Property(x => x.Reason)
            .HasMaxLength(100)
            .IsRequired();
    }
}