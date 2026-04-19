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
    public class PinServiceTests
    {
        private AppDbContext CreateDb()
        {
            var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

            db.Accounts.Add(new Account { Id = 1, OwnerName = "Test User", Balance = 500m });
            db.Cards.Add(new Card
            {
                Id = 1,
                CardNumber = "1234567890001111",
                PinHash = "1234",
                AccountId = 1
            });
            db.SaveChanges();
            return db;
        }

        [Fact]
        public async Task ChangePin_ValidData_UpdatesPin()
        {
            var db = CreateDb();
            var service = new PinService(db);

            await service.ChangePinAsync(1, "1234", "5678", "5678");

            db.Cards.Find(1)!.PinHash.Should().Be("5678");
        }

        [Fact]
        public async Task ChangePin_WrongCurrentPin_ThrowsException()
        {
            var db = CreateDb();
            var service = new PinService(db);

            var act = async () => await service.ChangePinAsync(1, "0000", "5678", "5678");

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*невірний*");
        }

        [Fact]
        public async Task ChangePin_PinMismatch_ThrowsException()
        {
            var db = CreateDb();
            var service = new PinService(db);

            var act = async () => await service.ChangePinAsync(1, "1234", "5678", "9999");

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*не збігаються*");
        }

        [Fact]
        public async Task ChangePin_SameAsCurrentPin_ThrowsException()
        {
            var db = CreateDb();
            var service = new PinService(db);

            var act = async () => await service.ChangePinAsync(1, "1234", "1234", "1234");

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*відрізнятись*");
        }

        [Fact]
        public async Task ChangePin_PinNotFourDigits_ThrowsException()
        {
            var db = CreateDb();
            var service = new PinService(db);

            var act = async () => await service.ChangePinAsync(1, "1234", "56", "56");

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*4 цифри*");
        }

        [Fact]
        public async Task ChangePin_PinWithLetters_ThrowsException()
        {
            var db = CreateDb();
            var service = new PinService(db);

            var act = async () => await service.ChangePinAsync(1, "1234", "ab12", "ab12");

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*4 цифри*");
        }

        [Fact]
        public async Task ChangePin_CardNotFound_ThrowsException()
        {
            var db = CreateDb();
            var service = new PinService(db);

            var act = async () => await service.ChangePinAsync(999, "1234", "5678", "5678");

            await act.Should().ThrowAsync<InvalidOperationException>()
                     .WithMessage("*не знайдено*");
        }
    }
}
