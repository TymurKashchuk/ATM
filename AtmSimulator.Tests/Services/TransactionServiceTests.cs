using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Patterns.Observer;
using AtmSimulator.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AtmSimulator.Tests.Services;

public class TransactionServiceTests
{
    private AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task GetHistoryAsync_ReturnsOnlyAccountTransactions_SortedDescending()
    {
        var db = CreateDb();
        db.Transactions.AddRange(
            new Transaction { AccountId = 1, Amount = 100, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new Transaction { AccountId = 1, Amount = 200, CreatedAt = DateTime.UtcNow },
            new Transaction { AccountId = 2, Amount = 999, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var service = new TransactionService(db, new Mock<TransactionNotifier>().Object);
        var result = await service.GetHistoryAsync(1);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(t => t.AccountId == 1);
        result.First().CreatedAt.Should().BeAfter(result.Last().CreatedAt);
    }

    [Fact]
    public async Task GetHistoryAsync_Pagination_WorksCorrectly()
    {
        var db = CreateDb();
        for (int i = 1; i <= 20; i++)
            db.Transactions.Add(new Transaction
            {
                AccountId = 1,
                Amount = i,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        await db.SaveChangesAsync();

        var service = new TransactionService(db, new Mock<TransactionNotifier>().Object);
        var page1 = await service.GetHistoryAsync(1, page: 1, pageSize: 10);
        var page2 = await service.GetHistoryAsync(1, page: 2, pageSize: 10);

        page1.Should().HaveCount(10);
        page2.Should().HaveCount(10);
        page1.Should().NotBeEquivalentTo(page2);
    }

    [Fact]
    public async Task GetHistoryAsync_NoTransactions_ReturnsEmptyList()
    {
        var db = CreateDb();
        var service = new TransactionService(db, new Mock<TransactionNotifier>().Object);

        var result = await service.GetHistoryAsync(999);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTotalCountAsync_ReturnsCorrectCount()
    {
        var db = CreateDb();
        db.Transactions.AddRange(
            new Transaction { AccountId = 1 },
            new Transaction { AccountId = 1 },
            new Transaction { AccountId = 2 }
        );
        await db.SaveChangesAsync();

        var service = new TransactionService(db, new Mock<TransactionNotifier>().Object);
        var count = await service.GetTotalCountAsync(1);

        count.Should().Be(2);
    }

    [Fact]
    public async Task GetTotalCountAsync_NoTransactions_ReturnsZero()
    {
        var db = CreateDb();
        var service = new TransactionService(db, new Mock<TransactionNotifier>().Object);

        var count = await service.GetTotalCountAsync(999);

        count.Should().Be(0);
    }

    [Fact]
    public async Task LogTransactionAsync_CallsNotifier()
    {
        var db = CreateDb();
        var received = false;

        var observer = new Mock<ITransactionObserver>();
        observer.Setup(o => o.OnTransactionAsync(It.IsAny<Transaction>()))
                .Callback(() => received = true)
                .Returns(Task.CompletedTask);

        var notifier = new TransactionNotifier();
        notifier.Subscribe(observer.Object);

        var service = new TransactionService(db, notifier);
        var transaction = new Transaction { AccountId = 1, Amount = 100 };
        await service.LogTransactionAsync(transaction);

        received.Should().BeTrue();
    }
}