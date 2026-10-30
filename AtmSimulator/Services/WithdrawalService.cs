using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Patterns.Strategy;
using Microsoft.EntityFrameworkCore;
using AtmSimulator.Patterns.Factory;
using AtmSimulator.Patterns.Observer;

namespace AtmSimulator.Services
{
    public class WithdrawalService
    {
        public const decimal DailyWithdrawalLimit = 20000m;
        public const int MaxBanknotesPerWithdrawal = 40;
        public const decimal WithdrawalFeePercent = 0.01m; // 1% комісія

        private readonly AppDbContext _context;
        private readonly ICashDispenserStrategy _dispenserStrategy;
        private readonly TransactionNotifier? _notifier;

        public WithdrawalService(
            AppDbContext context,
            ICashDispenserStrategy dispenserStrategy,
            TransactionNotifier? notifier = null
        )
        {
            _context = context;
            _dispenserStrategy = dispenserStrategy;
            _notifier = notifier;
        }

        public decimal CalculateFee(decimal amount) => Math.Round(amount * WithdrawalFeePercent, 2);

        public async Task<decimal> GetTodayWithdrawnAmountAsync(int accountId)
        {
            var todayStart = DateTime.UtcNow.Date;
            return await _context.Transactions
                .Where(t => t.AccountId == accountId &&
                            t.Type == TransactionType.Withdrawal &&
                            t.CreatedAt >= todayStart)
                .SumAsync(t => t.Amount);
        }

        public async Task<decimal> GetRemainingDailyLimitAsync(int accountId)
        {
            var todayWithdrawn = await GetTodayWithdrawnAmountAsync(accountId);
            return Math.Max(0, DailyWithdrawalLimit - todayWithdrawn);
        }

        public async Task<Dictionary<int, int>> GetAvailableCashAsync()
        {
            var cashStorage = await _context.CashStorage.ToListAsync();

            return cashStorage.ToDictionary(
                c => c.Denomination,
                c => c.Count
            );
        }

        public async Task<Dictionary<int, int>> WithdrawAsync(
            int accountId,
            decimal amount
        )
        {
            var account = await GetAccountAsync(accountId);

            ValidateWithdrawalAmount(amount);

            var fee = CalculateFee(amount);
            var totalDebit = amount + fee;

            ValidateBalance(account, totalDebit, fee);

            await ValidateDailyLimitAsync(accountId, amount);

            var availableCash = await GetAvailableCashAsync();

            var dispensed = _dispenserStrategy.Calculate(
                amount,
                availableCash
            );

            ValidateBanknoteLimit(dispensed);

            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? dbTransaction = null;
            if (_context.Database.IsRelational())
            {
                dbTransaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                account.Balance -= totalDebit;

                foreach (var (denomination, count) in dispensed)
                {
                    var cashEntry = await _context.CashStorage
                        .FirstAsync(c => c.Denomination == denomination);

                    cashEntry.Count -= count;
                }

                var dispensedSummary = string.Join(", ", dispensed.Select(d => $"{d.Value}x{d.Key}₴"));
                var description = fee > 0
                    ? $"Зняття готівки: {dispensedSummary} (Комісія 1%: {fee:N2} ₴)"
                    : $"Зняття готівки: {dispensedSummary}";

                var transaction = TransactionFactory.Create(
                    TransactionType.Withdrawal,
                    accountId,
                    amount,
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

                return dispensed;
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

        private async Task<Account> GetAccountAsync(int accountId)
        {
            var account = await _context.Accounts.FindAsync(accountId);

            if (account == null)
            {
                throw new InvalidOperationException(
                    "Акаунт не знайдено"
                );
            }

            return account;
        }

        private void ValidateBalance(Account account, decimal totalDebit, decimal fee)
        {
            if (account.Balance < totalDebit)
            {
                var feeMsg = fee > 0 ? $" з урахуванням комісії {fee:N2} ₴ (разом: {totalDebit:N2} ₴)" : "";
                throw new InvalidOperationException($"Недостатньо коштів{feeMsg}");
            }
        }

        private void ValidateWithdrawalAmount(decimal amount)
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException("Сума має бути більшою за 0");
            }

            if (amount % 20 != 0)
            {
                throw new InvalidOperationException(
                    "Сума має бути кратною 20"
                );
            }
        }

        private async Task ValidateDailyLimitAsync(int accountId, decimal amount)
        {
            var todayWithdrawn = await GetTodayWithdrawnAmountAsync(accountId);
            if (todayWithdrawn + amount > DailyWithdrawalLimit)
            {
                var remaining = Math.Max(0, DailyWithdrawalLimit - todayWithdrawn);
                throw new InvalidOperationException(
                    $"Перевищено добовий ліміт зняття ({DailyWithdrawalLimit:N2} ₴). " +
                    $"Сьогодні вже знято: {todayWithdrawn:N2} ₴. Доступний залишок: {remaining:N2} ₴");
            }
        }

        private void ValidateBanknoteLimit(Dictionary<int, int> dispensed)
        {
            var totalNotes = dispensed.Values.Sum();
            if (totalNotes > MaxBanknotesPerWithdrawal)
            {
                throw new InvalidOperationException(
                    $"Перевищено ліміт кількості купюр за одну операцію. " +
                    $"Для видачі потрібно {totalNotes} купюр (максимум {MaxBanknotesPerWithdrawal}). " +
                    $"Спробуйте обрати іншу суму");
            }
        }
    }
}
