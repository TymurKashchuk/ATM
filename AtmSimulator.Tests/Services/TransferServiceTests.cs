using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtmSimulator.Tests.Services
{
    public class TransferServiceTests
    {
        private AppDbContext CreateDb() {
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

            db.Accounts.AddRange(
                new Account { Id = 1, Balance = 1000m },
                new Account { Id = 2, Balance = 200m }
                );

            db.Cards.AddRange(
                new Card { Id = 1, CardNumber = "1234567890001111", AccountId = 1 },
                new Card { Id = 2, CardNumber = "1234567890002222", AccountId = 2 }
            );
            db.SaveChanges();
            return db;
        }

        [Fact]
        public async Task Transfer_ValidAccounts_CreateTwoTransactions() {
            var db = CreateDb();
            var servise = new TransferService(db);

            await servise.TransferAsync(1, "1234567890002222", 300m);
            db.Transactions.Count().Should().Be(2);
        }

        [Fact]
        public async Task Transfer_BalancesUpdatedCorrectly() {
            var db = CreateDb();
            var service = new TransferService(db);

            await service.TransferAsync(1, "1234567890002222", 300m);

            db.Accounts.Find(1)!.Balance.Should().Be(700m);
            db.Accounts.Find(2)!.Balance.Should().Be(500m);
        }

        [Fact]
        public async Task Transfer_SameAccount_ThrowsException()
        {
            var db = CreateDb();
            var service = new TransferService(db);

            var act = async () => await service.TransferAsync(1, "1234567890001111", 100m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*власний рахунок*");
        }

        [Fact]
        public async Task Transfer_InsufficientBalance_ThrowsException()
        {
            var db = CreateDb();
            var service = new TransferService(db);

            var act = async () => await service.TransferAsync(1, "1234567890002222", 5000m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*Недостатньо коштів*");
        }

        [Fact]
        public async Task Transfer_ZeroAmount_ThrowsException()
        {
            var db = CreateDb();
            var service = new TransferService(db);

            var act = async () => await service.TransferAsync(1, "1234567890002222", 0m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*більше 0*");
        }

        [Fact]
        public async Task Transfer_RecipientCardNotFound_ThrowsException()
        {
            var db = CreateDb();
            var service = new TransferService(db);

            var act = async () => await service.TransferAsync(1, "0000000000000000", 100m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*не знайдено*");
        }

        [Fact]
        public async Task Transfer_ToExternalCard_DeductsFeeFromSender()
        {
            var db = CreateDb();
            // Стороння картка (інший BIN - 987654 замість 123456)
            db.Accounts.Add(new Account { Id = 3, Balance = 100m });
            db.Cards.Add(new Card { Id = 3, CardNumber = "9876543210987654", AccountId = 3 });
            await db.SaveChangesAsync();

            var service = new TransferService(db);
            // Переказ 500 ₴. Комісія 2% = 10 ₴. Списання з відправника = 510 ₴.
            await service.TransferAsync(1, "9876543210987654", 500m);

            db.Accounts.Find(1)!.Balance.Should().Be(490m); // 1000 - 500 - 10
            db.Accounts.Find(3)!.Balance.Should().Be(600m); // 100 + 500

            var senderTx = db.Transactions.FirstOrDefault(t => t.AccountId == 1 && t.Description!.Contains("Комісія 2%"));
            senderTx.Should().NotBeNull();
        }

        [Fact]
        public async Task Transfer_ToExternalCard_InsufficientBalanceForFee_ThrowsException()
        {
            var db = CreateDb();
            db.Accounts.Add(new Account { Id = 3, Balance = 100m });
            db.Cards.Add(new Card { Id = 3, CardNumber = "9876543210987654", AccountId = 3 });
            await db.SaveChangesAsync();

            var service = new TransferService(db);
            // Баланс 1 = 1000. Переказ 1000. Комісія 2% = 20. Разом 1020 > 1000
            var act = async () => await service.TransferAsync(1, "9876543210987654", 1000m);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Недостатньо коштів з урахуванням комісії*");
        }
    }
}
