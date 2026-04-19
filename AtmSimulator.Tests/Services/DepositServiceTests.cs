using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Tests.Services
{
    public class DepositServiceTests
    {
        private AppDbContext CreateDb() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                 .UseInMemoryDatabase(Guid.NewGuid().ToString())
                 .Options);

        [Fact]
        public async Task Deposit_ValidAmount_IncreasesBalance() {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 500m });
            await db.SaveChangesAsync();

            var service = new DepositService(db);
            await service.DepositAsync(1, 300m);
            db.Accounts.Find(1)!.Balance.Should().Be(800m);
        }

        [Fact]
        public async Task Deposit_ZeroAmount_ThrowsException()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 500m });
            await db.SaveChangesAsync();

            var service = new DepositService(db);
            var act = async () => await service.DepositAsync(1, 0m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*більшою за 0*");
        }

        [Fact]
        public async Task Deposit_AmountNotMultipleOf20_ThrowsException()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 500m });
            await db.SaveChangesAsync();

            var service = new DepositService(db);
            var act = async () => await service.DepositAsync(1, 150m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*кратною 20*");
        }

        [Fact]
        public async Task Deposit_AccountNotFound_ThrowsException()
        {
            var db = CreateDb();

            var service = new DepositService(db);
            var act = async () => await service.DepositAsync(999, 200m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*не знайдено*");
        }

        [Fact]
        public async Task Deposit_CreatesTransactionRecord()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 500m });
            await db.SaveChangesAsync();

            var service = new DepositService(db);
            await service.DepositAsync(1, 400m);

            db.Transactions.Should().ContainSingle(t =>
                t.AccountId == 1 &&
                t.Type == TransactionType.Deposit &&
                t.Amount == 400m);
        }
    }
}
