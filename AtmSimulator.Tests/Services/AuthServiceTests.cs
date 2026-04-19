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
    public class AuthServiceTests
    {
        private AppDbContext CreateDb()
        {
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

            db.Accounts.Add(new Account { Id = 1, OwnerName = "Test User", Balance = 1000m });
            db.Cards.Add(new Card
            {
                Id = 1,
                CardNumber = "1234567890001111",
                PinHash = "1234",
                AccountId = 1,
                IsBlocked = false,
                FailedPinAttempts = 0
            });
            db.SaveChanges();
            return db;
        }

        [Fact]
        public async Task FindCardAsync_ExistingCard_ReturnsCard()
        {
            var db = CreateDb();
            var service = new AuthService(db);

            var card = await service.FindCardAsync("1234567890001111");

            card.Should().NotBeNull();
            card!.CardNumber.Should().Be("1234567890001111");
        }

        [Fact]
        public async Task FindCardAsync_NonExistingCard_ReturnsNull()
        {
            var db = CreateDb();
            var service = new AuthService(db);

            var card = await service.FindCardAsync("0000000000000000");

            card.Should().BeNull();
        }

        [Fact]
        public async Task ValidatePinAsync_CorrectPin_ReturnsTrue()
        {
            var db = CreateDb();
            var service = new AuthService(db);
            var card = await service.FindCardAsync("1234567890001111");

            var result = await service.ValidatePinAsync(card!, "1234");

            result.Should().BeTrue();
            card!.FailedPinAttempts.Should().Be(0);
        }

        [Fact]
        public async Task ValidatePinAsync_WrongPin_ReturnsFalse()
        {
            var db = CreateDb();
            var service = new AuthService(db);
            var card = await service.FindCardAsync("1234567890001111");

            var result = await service.ValidatePinAsync(card!, "0000");

            result.Should().BeFalse();
            card!.FailedPinAttempts.Should().Be(1);
        }

        [Fact]
        public async Task ValidatePinAsync_ThreeWrongAttempts_BlocksCard()
        {
            var db = CreateDb();
            var service = new AuthService(db);
            var card = await service.FindCardAsync("1234567890001111");

            await service.ValidatePinAsync(card!, "0000");
            await service.ValidatePinAsync(card!, "0000");
            await service.ValidatePinAsync(card!, "0000");

            card!.IsBlocked.Should().BeTrue();
        }

        [Fact]
        public async Task ValidatePinAsync_BlockedCard_ReturnsFalse()
        {
            var db = CreateDb();
            var card = db.Cards.Find(1)!;
            card.IsBlocked = true;
            await db.SaveChangesAsync();

            var service = new AuthService(db);
            var result = await service.ValidatePinAsync(card, "1234");

            result.Should().BeFalse();
        }

        [Fact]
        public void GetRemainingAttempts_NoFailedAttempts_ReturnsThree()
        {
            var db = CreateDb();
            var service = new AuthService(db);
            var card = db.Cards.Find(1)!;

            var remaining = service.GetRemainingAttempts(card);

            remaining.Should().Be(3);
        }

        [Fact]
        public async Task GetRemainingAttempts_OneFailedAttempt_ReturnsTwo()
        {
            var db = CreateDb();
            var service = new AuthService(db);
            var card = await service.FindCardAsync("1234567890001111");
            await service.ValidatePinAsync(card!, "0000");

            var remaining = service.GetRemainingAttempts(card!);

            remaining.Should().Be(2);
        }
    }
}
