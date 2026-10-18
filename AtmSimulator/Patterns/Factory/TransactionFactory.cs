using AtmSimulator.Models;

namespace AtmSimulator.Patterns.Factory
{
    public class TransactionFactory
    {
        public static Transaction Create(TransactionType type, int accountId, decimal amount, string? description = null)
        {
            return type switch
            {
                TransactionType.Withdrawal => CreateWithdrawal(accountId, amount, description),
                TransactionType.Deposit => CreateDeposit(accountId, amount, description),
                TransactionType.Transfer => CreateTransfer(accountId, amount, description),
                _ => throw new ArgumentException($"Unknown transaction type: {type}")
            };
        }

        private static Transaction CreateWithdrawal(int accountId, decimal amount, string? description)
        {
            return new Transaction
            {
                Type = TransactionType.Withdrawal,
                AccountId = accountId,
                Amount = amount,
                Description = description ?? $"ATM Withdrawal of {amount:N2} ₴",
                CreatedAt = DateTime.UtcNow
            };
        }

        private static Transaction CreateDeposit(int accountId, decimal amount, string? description)
        {
            return new Transaction
            {
                Type = TransactionType.Deposit,
                AccountId = accountId,
                Amount = amount,
                Description = description ?? $"ATM Deposit of {amount:N2} ₴",
                CreatedAt = DateTime.UtcNow
            };
        }

        private static Transaction CreateTransfer(int accountId, decimal amount, string? description)
        {
            return new Transaction
            {
                Type = TransactionType.Transfer,
                AccountId = accountId,
                Amount = amount,
                Description = description ?? $"Transfer of {amount:N2} ₴",
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
