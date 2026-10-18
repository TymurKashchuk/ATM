using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Patterns.Factory;
using AtmSimulator.Patterns.Observer;

namespace AtmSimulator.Services
{
    public class DepositService
    {
        private readonly AppDbContext _context;
        private readonly TransactionNotifier? _notifier;

        public DepositService(AppDbContext context, TransactionNotifier? notifier = null)
        {
            _context = context;
            _notifier = notifier;
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

            var transaction = TransactionFactory.Create(TransactionType.Deposit, accountId, amount);
            _context.Transactions.Add(transaction);

            await _context.SaveChangesAsync();

            if (_notifier != null)
            {
                await _notifier.NotifyAsync(transaction);
            }
        }
    }
}
