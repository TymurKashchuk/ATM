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

        [Fact]
        public async Task Deposit_WithBanknotes_IncreasesBalanceAndCashStorage()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 1000m });
            db.CashStorage.AddRange(
                new CashStorage { Id = 1, Denomination = 500, Count = 10 },
                new CashStorage { Id = 2, Denomination = 200, Count = 20 }
            );
            await db.SaveChangesAsync();

            var service = new DepositService(db);
            var banknotes = new Dictionary<int, int>
            {
                { 500, 2 },
                { 200, 3 }
            };

            var depositedAmount = await service.DepositAsync(1, banknotes);

            depositedAmount.Should().Be(1600m);
            db.Accounts.Find(1)!.Balance.Should().Be(2600m);

            db.CashStorage.First(c => c.Denomination == 500).Count.Should().Be(12);
            db.CashStorage.First(c => c.Denomination == 200).Count.Should().Be(23);

            var tx = db.Transactions.FirstOrDefault(t => t.AccountId == 1 && t.Type == TransactionType.Deposit);
            tx.Should().NotBeNull();
            tx!.Amount.Should().Be(1600m);
            tx.Description.Should().Contain("2x500₴").And.Contain("3x200₴");
        }

        [Fact]
        public async Task Deposit_WithNoBanknotes_ThrowsException()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 500m });
            await db.SaveChangesAsync();

            var service = new DepositService(db);
            var act = async () => await service.DepositAsync(1, new Dictionary<int, int>());

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Не вказано купюри*");
        }

        [Fact]
        public async Task Deposit_WithNegativeBanknoteCount_ThrowsException()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 500m });
            await db.SaveChangesAsync();

            var service = new DepositService(db);
            var act = async () => await service.DepositAsync(1, new Dictionary<int, int> { { 500, -1 } });

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*від'ємною*");
        }

        [Fact]
        public async Task Deposit_WithUnsupportedDenomination_ThrowsException()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 500m });
            db.CashStorage.Add(new CashStorage { Id = 1, Denomination = 500, Count = 10 });
            await db.SaveChangesAsync();

            var service = new DepositService(db);
            var act = async () => await service.DepositAsync(1, new Dictionary<int, int> { { 1000, 1 } });

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*не підтримується*");
        }
    }
}
