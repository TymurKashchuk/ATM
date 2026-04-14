using AtmSimulator.Data;
using AtmSimulator.Patterns.Observer;
using Microsoft.EntityFrameworkCore;
using AtmSimulator.Models;

namespace AtmSimulator.Services
{
    public class TransactionService
    {
        private readonly AppDbContext _context;
        private readonly TransactionNotifier _notifier;

        public TransactionService(AppDbContext context, TransactionNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        public async Task<List<Transaction>> GetHistoryAsync(int accountId, int page = 1, int pageSize = 10)
        {
            return await _context.Transactions
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalCountAsync(int accountId)
        {
            return await _context.Transactions
                .CountAsync(t => t.AccountId == accountId);
        }

        public async Task LogTransactionAsync(Transaction transaction)
        {
            await _notifier.NotifyAsync(transaction);
        }
    }
}
