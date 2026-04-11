using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Patterns.Strategy;
using Microsoft.EntityFrameworkCore;

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
                ?? throw new InvalidOperationException("Account not found");

            if (account.Balance < amount)
                throw new InvalidOperationException("Insufficient funds");

            if (amount % 20 != 0)
                throw new InvalidOperationException("Amount must be a multiple of 20");

            var availableCash = await GetAvailableCashAsync();
            var dispensed = _dispenserStrategy.Calculate(amount, availableCash);

            account.Balance -= amount;

            foreach (var (denomination, count) in dispensed)
            {
                var cashEntry = await _context.CashStorage
                    .FirstAsync(c => c.Denomination == denomination);
                cashEntry.Count -= count;
            }

            _context.Transactions.Add(new Transaction
            {
                AccountId = accountId,
                Type = TransactionType.Withdrawal,
                Amount = amount,
                Description = $"Cash withdrawal: {string.Join(", ", dispensed.Select(d => $"{d.Value}x{d.Key}₴"))}"
            });

            await _context.SaveChangesAsync();
            return dispensed;
        }
    }
}
