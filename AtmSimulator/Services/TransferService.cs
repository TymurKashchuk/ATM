using AtmSimulator.Data;
using AtmSimulator.Models;
using Microsoft.EntityFrameworkCore;
using AtmSimulator.Patterns.Factory;

namespace AtmSimulator.Services
{
    public class TransferService
    {
        private readonly AppDbContext _context;

        public TransferService(AppDbContext context)
        {
            _context = context;
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

            _context.Transactions.Add(
                TransactionFactory.Create(TransactionType.Transfer, senderAccountId, amount,
                    $"Переказ на картку **** {recipientCardNumber[^4..]}: -{amount} ₴")
            );

            _context.Transactions.Add(
                TransactionFactory.Create(TransactionType.Transfer, recipient.Id, amount,
                    $"Переказ з картки **** {GetSenderCardSuffix(senderAccountId)}: +{amount} ₴")
            );

            await _context.SaveChangesAsync();
        }

        private string GetSenderCardSuffix(int accountId)
        {
            var card = _context.Cards.FirstOrDefault(c => c.AccountId == accountId);
            return card?.CardNumber[^4..] ?? "****";
        }
    }
}
