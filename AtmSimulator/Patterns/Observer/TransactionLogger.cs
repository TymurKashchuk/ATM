using System.Transactions;
using AtmSimulator.Models;

namespace AtmSimulator.Patterns.Observer
{
    public class TransactionLogger : ITransactionObserver
    {
        private readonly ILogger<TransactionLogger> _logger;

        public TransactionLogger(ILogger<TransactionLogger> logger)
        {
            _logger = logger;
        }

        public Task OnTransactionAsync(Transaction transaction) {
            _logger.LogInformation(
                 "[TRANSACTION] Type: {Type} | Amount: {Amount} ₴ | Account: {AccountId} | Time: {Time} | {Description}",
                transaction.Type,
                transaction.Amount,
                transaction.AccountId,
                transaction.CreatedAt,
                transaction.Description
                );

            return Task.CompletedTask;
        }
    }
}
