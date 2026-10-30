using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Tests.Services
{
    public class ReceiptServiceTests
    {
        private AppDbContext CreateDb() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                 .UseInMemoryDatabase(Guid.NewGuid().ToString())
                 .Options);

        [Fact]
        public async Task GenerateReceipt_ValidWithdrawalTransaction_ReturnsFormattedReceipt()
        {
            var db = CreateDb();
            var account = new Account { Id = 1, OwnerName = "Tymur Kashchuk", Balance = 8990m };
            var card = new Card { Id = 1, CardNumber = "1234567890123456", AccountId = 1 };
            var tx = new Transaction
            {
                Id = 42,
                AccountId = 1,
                Type = TransactionType.Withdrawal,
                Amount = 1000m,
                Description = "Зняття готівки: 2x500₴ (Комісія 1%: 10.00 ₴)",
                CreatedAt = new DateTime(2026, 10, 30, 15, 0, 0, DateTimeKind.Utc)
            };

            db.Accounts.Add(account);
            db.Cards.Add(card);
            db.Transactions.Add(tx);
            await db.SaveChangesAsync();

            var service = new ReceiptService(db);
            var receipt = await service.GenerateReceiptAsync(42);

            receipt.TransactionId.Should().Be(42);
            receipt.TransactionType.Should().Be(TransactionType.Withdrawal);
            receipt.OwnerName.Should().Be("Tymur Kashchuk");
            receipt.CardNumberMasked.Should().Be("**** **** **** 3456");
            receipt.Amount.Should().Be(1000m);
            receipt.Fee.Should().Be(10m);
            receipt.TotalDebit.Should().Be(1010m);
            receipt.RemainingBalance.Should().Be(8990m);
            receipt.TerminalId.Should().Be("ATM-UA-042");
            receipt.FormattedTransactionType.Should().Contain("Зняття готівки");
        }

        [Fact]
        public async Task GenerateReceipt_DepositTransaction_ReturnsCorrectValues()
        {
            var db = CreateDb();
            var account = new Account { Id = 2, OwnerName = "Anna Kowalska", Balance = 5000m };
            var card = new Card { Id = 2, CardNumber = "9876543210987654", AccountId = 2 };
            var tx = new Transaction
            {
                Id = 101,
                AccountId = 2,
                Type = TransactionType.Deposit,
                Amount = 2000m,
                Description = "Поповнення готівкою: 4x500₴",
                CreatedAt = DateTime.UtcNow
            };

            db.Accounts.Add(account);
            db.Cards.Add(card);
            db.Transactions.Add(tx);
            await db.SaveChangesAsync();

            var service = new ReceiptService(db);
            var receipt = await service.GenerateReceiptAsync(101);

            receipt.TransactionId.Should().Be(101);
            receipt.Amount.Should().Be(2000m);
            receipt.Fee.Should().Be(0m);
            receipt.TotalDebit.Should().Be(2000m);
            receipt.CardNumberMasked.Should().Be("**** **** **** 7654");
            receipt.FormattedTransactionType.Should().Contain("Поповнення");
        }

        [Fact]
        public async Task GenerateReceipt_NonExistentTransaction_ThrowsException()
        {
            var db = CreateDb();
            var service = new ReceiptService(db);

            var act = async () => await service.GenerateReceiptAsync(999);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*не знайдено*");
        }
    }
}
