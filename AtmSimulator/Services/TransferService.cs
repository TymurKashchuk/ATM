using AtmSimulator.Data;
using AtmSimulator.Models;
using Microsoft.EntityFrameworkCore;
using AtmSimulator.Patterns.Factory;
using AtmSimulator.Patterns.Observer;

namespace AtmSimulator.Services
{
    public class TransferService
    {
        private readonly AppDbContext _context;
        private readonly TransactionNotifier? _notifier;

        public TransferService(AppDbContext context, TransactionNotifier? notifier = null)
        {
            _context = context;
            _notifier = notifier;
        }

        public async Task<Account> FindRecipientAsync(string cardNumber)
        {
            var card = await _context.Cards
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CardNumber == cardNumber)
                ?? throw new InvalidOperationException("Картку отримувача не знайдено");

            return card.Account!;
        }

        public async Task TransferAsync(int senderAccountId, string recipientCardNumber, decimal amount)
        {
            if (amount <= 0)
                throw new InvalidOperationException("Сума має бути більше 0");

            var sender = await _context.Accounts.FindAsync(senderAccountId)
                ?? throw new InvalidOperationException("Рахунок відправника не знайдено");

            var recipientCard = await _context.Cards
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CardNumber == recipientCardNumber)
                ?? throw new InvalidOperationException("Картку отримувача не знайдено");

            if (recipientCard.AccountId == senderAccountId)
                throw new InvalidOperationException("Неможливо переказати на власний рахунок");

            if (sender.Balance < amount)
                throw new InvalidOperationException("Недостатньо коштів");

            var recipient = recipientCard.Account!;

            sender.Balance -= amount;
            recipient.Balance += amount;

            var senderTransaction = TransactionFactory.Create(TransactionType.Transfer, senderAccountId, amount,
                $"Переказ на картку **** {recipientCardNumber[^4..]}: -{amount} ₴");

            var recipientTransaction = TransactionFactory.Create(TransactionType.Transfer, recipient.Id, amount,
                $"Переказ з картки **** {GetSenderCardSuffix(senderAccountId)}: +{amount} ₴");

            _context.Transactions.Add(senderTransaction);
            _context.Transactions.Add(recipientTransaction);

            await _context.SaveChangesAsync();

            if (_notifier != null)
            {
                await _notifier.NotifyAsync(senderTransaction);
                await _notifier.NotifyAsync(recipientTransaction);
            }
        }

        private string GetSenderCardSuffix(int accountId)
        {
            var card = _context.Cards.FirstOrDefault(c => c.AccountId == accountId);
            return card?.CardNumber[^4..] ?? "****";
        }
    }
}
