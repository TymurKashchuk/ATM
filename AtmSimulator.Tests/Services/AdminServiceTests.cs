using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Tests.Services
{
    public class AdminServiceTests
    {
        private AppDbContext CreateDb() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                 .UseInMemoryDatabase(Guid.NewGuid().ToString())
                 .Options);

        [Fact]
        public async Task GetCashStorageDetails_ReturnsCorrectCassettesAndTotals()
        {
            var db = CreateDb();
            db.CashStorage.AddRange(
                new CashStorage { Id = 1, Denomination = 500, Count = 10 }, // 5000
                new CashStorage { Id = 2, Denomination = 200, Count = 20 }  // 4000
            );
            await db.SaveChangesAsync();

            var service = new AdminService(db);
            var result = await service.GetCashStorageDetailsAsync();

            result.Cassettes.Should().HaveCount(2);
            result.TotalBanknotesCount.Should().Be(30);
            result.TotalCashAmount.Should().Be(9000m);
        }

        [Fact]
        public async Task RefillCassette_ValidParameters_IncreasesCount()
        {
            var db = CreateDb();
            db.CashStorage.Add(new CashStorage { Id = 1, Denomination = 100, Count = 5 });
            await db.SaveChangesAsync();

            var service = new AdminService(db);
            await service.RefillCassetteAsync(100, 15);

            var entry = await db.CashStorage.FirstAsync(c => c.Denomination == 100);
            entry.Count.Should().Be(20);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task RefillCassette_InvalidCount_ThrowsException(int count)
        {
            var db = CreateDb();
            db.CashStorage.Add(new CashStorage { Id = 1, Denomination = 100, Count = 5 });
            await db.SaveChangesAsync();

            var service = new AdminService(db);
            var act = async () => await service.RefillCassetteAsync(100, count);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*більшою за 0*");
        }

        [Fact]
        public async Task RefillCassette_NonExistentDenomination_ThrowsException()
        {
            var db = CreateDb();
            var service = new AdminService(db);

            var act = async () => await service.RefillCassetteAsync(500, 10);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*не знайдено*");
        }

        [Fact]
        public async Task WithdrawCassette_ValidParameters_DecreasesCount()
        {
            var db = CreateDb();
            db.CashStorage.Add(new CashStorage { Id = 1, Denomination = 200, Count = 10 });
            await db.SaveChangesAsync();

            var service = new AdminService(db);
            await service.WithdrawCassetteAsync(200, 4);

            var entry = await db.CashStorage.FirstAsync(c => c.Denomination == 200);
            entry.Count.Should().Be(6);
        }

        [Fact]
        public async Task WithdrawCassette_MoreThanAvailable_ThrowsException()
        {
            var db = CreateDb();
            db.CashStorage.Add(new CashStorage { Id = 1, Denomination = 50, Count = 5 });
            await db.SaveChangesAsync();

            var service = new AdminService(db);
            var act = async () => await service.WithdrawCassetteAsync(50, 6);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Неможливо вилучити*залишилось лише 5*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public async Task WithdrawCassette_InvalidCount_ThrowsException(int count)
        {
            var db = CreateDb();
            db.CashStorage.Add(new CashStorage { Id = 1, Denomination = 50, Count = 5 });
            await db.SaveChangesAsync();

            var service = new AdminService(db);
            var act = async () => await service.WithdrawCassetteAsync(50, count);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*більшою за 0*");
        }
    }
}
