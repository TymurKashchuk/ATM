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
        public async Task Withdraw_SufficientBalance_DeductsAmountAndFee()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 1000m });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            await service.WithdrawAsync(1, 500m);

            // 1000m - 500m (сума) - 5m (комісія 1%) = 495m
            db.Accounts.Find(1)!.Balance.Should().Be(495m);
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

        [Fact]
        public async Task Withdraw_WithObserver_NotifiesObserver()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 1000m });
            await db.SaveChangesAsync();

            var observerMock = new Mock<AtmSimulator.Patterns.Observer.ITransactionObserver>();
            var notifier = new AtmSimulator.Patterns.Observer.TransactionNotifier();
            notifier.Subscribe(observerMock.Object);

            var strategy = new GreedyCashDispenserStrategy();
            var service = new WithdrawalService(db, strategy, notifier);

            await service.WithdrawAsync(1, 200m);

            observerMock.Verify(o => o.OnTransactionAsync(It.Is<Transaction>(t =>
                t.AccountId == 1 &&
                t.Type == TransactionType.Withdrawal &&
                t.Amount == 200m)), Times.Once);
        }

        [Fact]
        public async Task Withdraw_ExceedingDailyLimit_ThrowsException()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 30000m });
            db.Transactions.Add(new Transaction
            {
                AccountId = 1,
                Type = TransactionType.Withdrawal,
                Amount = 19000m,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var act = async () => await service.WithdrawAsync(1, 1200m); // 19000 + 1200 = 20200 > 20000

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Перевищено добовий ліміт зняття*");
        }

        [Fact]
        public async Task Withdraw_ExceedingMaxBanknotesLimit_ThrowsException()
        {
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

            // Лише 20-ки: для 1000 грн потрібно 50 купюр (> 40)
            db.CashStorage.Add(new CashStorage { Denomination = 20, Count = 100 });
            db.Accounts.Add(new Account { Id = 1, Balance = 5000m });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var act = async () => await service.WithdrawAsync(1, 1000m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Перевищено ліміт кількості купюр*");
        }

        [Fact]
        public async Task Withdraw_InsufficientBalanceForFee_ThrowsException()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 1, Balance = 500m });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            // Сума 500 + 5 (комісія) = 505 > 500
            var act = async () => await service.WithdrawAsync(1, 500m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Недостатньо коштів з урахуванням комісії*");
        }
    }
}
