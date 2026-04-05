using AtmSimulator.Models;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Account> Accounts { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<CashStorage> CashStorage { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Card>()
            .HasIndex(c => c.CardNumber)
            .IsUnique();

        modelBuilder.Entity<Account>()
            .Property(a => a.Balance)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<CashStorage>()
            .HasIndex(c => c.Denomination)
            .IsUnique();

        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>().HasData(
            new Account { Id = 1, OwnerName = "John Jones", Balance = 10000m, CreatedAt = DateTime.UtcNow },
            new Account { Id = 2, OwnerName = "Will Smith", Balance = 5000m, CreatedAt = DateTime.UtcNow }
        );

        modelBuilder.Entity<Card>().HasData(
            new Card { Id = 1, CardNumber = "1234567890123456", PinHash = "1234", IsBlocked = false, FailedPinAttempts = 0, ExpiryDate = DateTime.UtcNow.AddYears(3), AccountId = 1 },
            new Card { Id = 2, CardNumber = "6543210987654321", PinHash = "4321", IsBlocked = false, FailedPinAttempts = 0, ExpiryDate = DateTime.UtcNow.AddYears(3), AccountId = 2 }
        );

        modelBuilder.Entity<CashStorage>().HasData(
            new CashStorage { Id = 1, Denomination = 500, Count = 100 },
            new CashStorage { Id = 2, Denomination = 200, Count = 100 },
            new CashStorage { Id = 3, Denomination = 100, Count = 100 },
            new CashStorage { Id = 4, Denomination = 50, Count = 100 },
            new CashStorage { Id = 5, Denomination = 20, Count = 100 }
        );
    }
}