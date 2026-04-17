using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Patterns.Strategy;
using Microsoft.EntityFrameworkCore;
using AtmSimulator.Patterns.Factory;

namespace AtmSimulator.Services
{
    public class WithdrawalService
    {
        private readonly AppDbContext _context;
        private readonly ICashDispenserStrategy _dispenserStrategy;

        public WithdrawalService(AppDbContext context, ICashDispenserStrategy dispenserStrategy)
        {
            _context = context;
            _dispenserStrategy = dispenserStrategy;
        }

        public async Task<Dictionary<int, int>> GetAvailableCashAsync() {
            var cashStorage = await _context.CashStorage.ToListAsync();
            return cashStorage.ToDictionary(c => c.Denomination, c => c.Count);
        }

        public async Task<Dictionary<int, int>> WithdrawAsync(int accountId, decimal amount) {
            var account = await _context.Accounts.FindAsync(accountId)
                ?? throw new InvalidOperationException("Акаунт не знайдено");

            if (account.Balance < amount)
                throw new InvalidOperationException("Недостатньо коштів");

            if (amount % 20 != 0)
                throw new InvalidOperationException("Сума має бути кратною 20");

            var availableCash = await GetAvailableCashAsync();
            var dispensed = _dispenserStrategy.Calculate(amount, availableCash);

            account.Balance -= amount;

            foreach (var (denomination, count) in dispensed)
            {
                var cashEntry = await _context.CashStorage
                    .FirstAsync(c => c.Denomination == denomination);
                cashEntry.Count -= count;
            }

            var description = $"Зняття готівки: {string.Join(", ", dispensed.Select(d => $"{d.Value}x{d.Key}₴"))}";
            _context.Transactions.Add(TransactionFactory.Create(TransactionType.Withdrawal, accountId, amount, description));

            await _context.SaveChangesAsync();
            return dispensed;
        }
    }
}
