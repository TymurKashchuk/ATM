using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Patterns.Factory;
using AtmSimulator.Patterns.Observer;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<int>> GetSupportedDenominationsAsync()
        {
            var denominations = await _context.CashStorage
                .Select(c => c.Denomination)
                .OrderByDescending(d => d)
                .ToListAsync();

            if (!denominations.Any())
            {
                return new List<int> { 500, 200, 100, 50, 20 };
            }

            return denominations;
        }

        public async Task<decimal> DepositAsync(int accountId, Dictionary<int, int> banknotes)
        {
            if (banknotes == null || !banknotes.Any())
                throw new InvalidOperationException("Не вказано купюри для поповнення");

            if (banknotes.Any(b => b.Value < 0))
                throw new InvalidOperationException("Кількість купюр не може бути від'ємною");

            var totalAmount = banknotes.Sum(b => (decimal)b.Key * b.Value);

            if (totalAmount <= 0)
                throw new InvalidOperationException("Сума для внесення має бути більшою за 0");

            var account = await _context.Accounts.FindAsync(accountId)
                ?? throw new InvalidOperationException("Обліковий запис не знайдено");

            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? dbTransaction = null;
            if (_context.Database.IsRelational())
            {
                dbTransaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                foreach (var (denomination, count) in banknotes.Where(b => b.Value > 0))
                {
                    var cashEntry = await _context.CashStorage
                        .FirstOrDefaultAsync(c => c.Denomination == denomination);

                    if (cashEntry == null)
                    {
                        throw new InvalidOperationException($"Номінал {denomination} ₴ не підтримується банкоматом");
                    }

                    cashEntry.Count += count;
                }

                account.Balance += totalAmount;

                var billsSummary = string.Join(", ", banknotes
                    .Where(b => b.Value > 0)
                    .OrderByDescending(b => b.Key)
                    .Select(b => $"{b.Value}x{b.Key}₴"));

                var description = $"Поповнення готівкою: {billsSummary}";

                var transaction = TransactionFactory.Create(
                    TransactionType.Deposit,
                    accountId,
                    totalAmount,
                    description
                );

                _context.Transactions.Add(transaction);

                await _context.SaveChangesAsync();

                if (dbTransaction != null)
                {
                    await dbTransaction.CommitAsync();
                }

                if (_notifier != null)
                {
                    await _notifier.NotifyAsync(transaction);
                }

                return totalAmount;
            }
            catch
            {
                if (dbTransaction != null)
                {
                    await dbTransaction.RollbackAsync();
                }
                throw;
            }
            finally
            {
                if (dbTransaction != null)
                {
                    await dbTransaction.DisposeAsync();
                }
            }
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
