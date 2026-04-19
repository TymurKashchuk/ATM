using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Services;
using AtmSimulator.Patterns.Strategy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AtmSimulator.Tests.Services
{
    public class WithdrawalServiceTests
    {
        private AppDbContext CreateDb()
        {
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

            db.CashStorage.AddRange(
                new CashStorage { Denomination = 1000, Count = 10 },
                new CashStorage { Denomination = 500, Count = 10 },
                new CashStorage { Denomination = 200, Count = 10 },
                new CashStorage { Denomination = 100, Count = 10 },
                new CashStorage { Denomination = 50, Count = 10 },
                new CashStorage { Denomination = 20, Count = 10 }
            );
            db.SaveChanges();
            return db;
        }

        private WithdrawalService CreateService(AppDbContext db)
        {
            var strategy = new GreedyCashDispenserStrategy();
            return new WithdrawalService(db, strategy);
        }

        [Fact]
        public async Task Withdraw_SufficientBalance_DeductsAmount()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 1000m });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            await service.WithdrawAsync(1, 500m);

            db.Accounts.Find(1)!.Balance.Should().Be(500m);
        }

        [Fact]
        public async Task Withdraw_SufficientBalance_ReturnsDispensedBills()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 1000m });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var result = await service.WithdrawAsync(1, 500m);

            result.Should().NotBeEmpty();
            result.Values.Sum(v => v).Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Withdraw_InsufficientBalance_ThrowsException()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 100m });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var act = async () => await service.WithdrawAsync(1, 500m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*Недостатньо коштів*");
        }

        [Fact]
        public async Task Withdraw_AmountNotMultipleOf20_ThrowsException()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 1000m });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var act = async () => await service.WithdrawAsync(1, 150m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*кратною 20*");
        }

        [Fact]
        public async Task Withdraw_AccountNotFound_ThrowsException()
        {
            var db = CreateDb();

            var service = CreateService(db);
            var act = async () => await service.WithdrawAsync(999, 100m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*не знайдено*");
        }

        [Fact]
        public async Task Withdraw_CreatesTransactionRecord()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 1000m });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            await service.WithdrawAsync(1, 500m);

            db.Transactions.Should().ContainSingle(t =>
                t.AccountId == 1 &&
                t.Type == TransactionType.Withdrawal &&
                t.Amount == 500m);
        }
    }
}