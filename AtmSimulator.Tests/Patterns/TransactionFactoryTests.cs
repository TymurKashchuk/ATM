using AtmSimulator.Models;
using AtmSimulator.Patterns.Factory;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtmSimulator.Tests.Patterns
{
    public class TransactionFactoryTests
    {
        [Fact]
        public void CreateWithdrawal_ReturnsCorrectType()
        {
            var transaction = TransactionFactory.Create(TransactionType.Withdrawal, 1, 500m);

            transaction.Type.Should().Be(TransactionType.Withdrawal);
            transaction.Amount.Should().Be(500m);
            transaction.AccountId.Should().Be(1);
        }

        [Fact]
        public void CreateDeposit_ReturnsCorrectType()
        {
            var transaction = TransactionFactory.Create(TransactionType.Deposit, 1, 1000m);

            transaction.Type.Should().Be(TransactionType.Deposit);
            transaction.Amount.Should().Be(1000m);
        }

        [Fact]
        public void CreateTransfer_SetsDescription()
        {
            var transaction = TransactionFactory.Create(
                TransactionType.Transfer, 1, 200m, "Переказ на acc2");

            transaction.Type.Should().Be(TransactionType.Transfer);
            transaction.Description.Should().Contain("acc2");
        }

        [Fact]
        public void Create_SetsCreatedAtToNow()
        {
            var before = DateTime.UtcNow;
            var transaction = TransactionFactory.Create(TransactionType.Deposit, 1, 100m);
            var after = DateTime.UtcNow;

            transaction.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }
    }
}
