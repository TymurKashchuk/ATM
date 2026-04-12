using AtmSimulator.Data;
using AtmSimulator.Models;

namespace AtmSimulator.Services
{
    public class DepositService
    {
        private readonly AppDbContext _context;

        public DepositService(AppDbContext context)
        {
            _context = context;
        }

        public async Task DepositAsync(int accountId, decimal amount)
        {
            var account = await _context.Accounts.FindAsync(accountId)
                ?? throw new InvalidOperationException("Обліковий запис не знайдено");

            if (amount <= 0)
                throw new InvalidOperationException("Сума має бути більшою за 0");

            if (amount % 20 != 0)
                throw new InvalidOperationException("Сума має бути кратною 20");

            account.Balance += amount;

            _context.Transactions.Add(new Transaction
            {
                AccountId = accountId,
                Type = TransactionType.Deposit,
                Amount = amount,
                Description = $"Грошовий депозит: {amount} ₴"
            });

            await _context.SaveChangesAsync();
        }
    }
}
