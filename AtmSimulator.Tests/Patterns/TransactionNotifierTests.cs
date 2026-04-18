using AtmSimulator.Models;
using AtmSimulator.Patterns.Observer;
using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtmSimulator.Tests.Patterns
{
    public class TransactionNotifierTests
    {
        [Fact]
        public async Task NotifyAsync_CallsAllObservers()
        {
            var observer1 = new Mock<ITransactionObserver>();
            var observer2 = new Mock<ITransactionObserver>();
            var notifier = new TransactionNotifier();
            notifier.Subscribe(observer1.Object);
            notifier.Subscribe(observer2.Object);

            var transaction = new Transaction
            {
                Type = TransactionType.Deposit,
                Amount = 100m,
                AccountId = 1,
                CreatedAt = DateTime.UtcNow
            };

            await notifier.NotifyAsync(transaction);

            observer1.Verify(o => o.OnTransactionAsync(transaction), Times.Once);
            observer2.Verify(o => o.OnTransactionAsync(transaction), Times.Once);
        }

        [Fact]
        public async Task NotifyAsync_NoObservers_DoesNotThrow()
        {
            var notifier = new TransactionNotifier();
            var transaction = new Transaction { Amount = 100m, AccountId = 1 };

            var act = async () => await notifier.NotifyAsync(transaction);

            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task Subscribe_MultipleObservers_AllReceiveNotification()
        {
            var receivedCount = 0;
            var observer = new Mock<ITransactionObserver>();
            observer.Setup(o => o.OnTransactionAsync(It.IsAny<Transaction>()))
                    .Callback(() => receivedCount++)
                    .Returns(Task.CompletedTask);

            var notifier = new TransactionNotifier();
            notifier.Subscribe(observer.Object);
            notifier.Subscribe(observer.Object);

            await notifier.NotifyAsync(new Transaction { AccountId = 1 });

            receivedCount.Should().Be(2);
        }
    }
}
