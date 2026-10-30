using AtmSimulator.Data;
using AtmSimulator.Models;
using Microsoft.EntityFrameworkCore;
using AtmSimulator.Patterns.Factory;
using AtmSimulator.Patterns.Observer;

namespace AtmSimulator.Services
{
    public class TransferService
    {
        public const decimal ExternalTransferFeePercent = 0.02m; // 2% для сторонніх карток
        public const decimal InternalTransferFeePercent = 0.00m; // 0% для власних карток

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

        public bool IsExternalCard(string senderCardNumber, string recipientCardNumber)
        {
            if (string.IsNullOrWhiteSpace(senderCardNumber) || string.IsNullOrWhiteSpace(recipientCardNumber))
                return true;

            var senderBin = senderCardNumber.Length >= 6 ? senderCardNumber[..6] : senderCardNumber;
            var recipientBin = recipientCardNumber.Length >= 6 ? recipientCardNumber[..6] : recipientCardNumber;

            return senderBin != recipientBin;
        }

        public decimal CalculateTransferFee(string senderCardNumber, string recipientCardNumber, decimal amount)
        {
            var isExternal = IsExternalCard(senderCardNumber, recipientCardNumber);
            var rate = isExternal ? ExternalTransferFeePercent : InternalTransferFeePercent;
            return Math.Round(amount * rate, 2);
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

            var senderCard = await _context.Cards.FirstOrDefaultAsync(c => c.AccountId == senderAccountId);
            var senderCardNumber = senderCard?.CardNumber ?? "";
            var senderCardSuffix = senderCardNumber.Length >= 4
                ? senderCardNumber[^4..]
                : "****";

            var isExternal = IsExternalCard(senderCardNumber, recipientCardNumber);
            var fee = CalculateTransferFee(senderCardNumber, recipientCardNumber, amount);
            var totalDebit = amount + fee;

            if (sender.Balance < totalDebit)
            {
                var feeMsg = fee > 0 ? $" з урахуванням комісії {fee:N2} ₴ (разом: {totalDebit:N2} ₴)" : "";
                throw new InvalidOperationException($"Недостатньо коштів{feeMsg}");
            }

            var recipient = recipientCard.Account!;

            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? dbTransaction = null;
            if (_context.Database.IsRelational())
            {
                dbTransaction = await _context.Database.BeginTransactionAsync();
            }

            try
            {
                sender.Balance -= totalDebit;
                recipient.Balance += amount;

                var senderDescription = isExternal
                    ? $"Переказ на сторонню картку **** {recipientCardNumber[^4..]}: -{amount:N2} ₴ (Комісія 2%: {fee:N2} ₴)"
                    : $"Переказ на картку **** {recipientCardNumber[^4..]}: -{amount:N2} ₴";

                var recipientDescription = $"Переказ з картки **** {senderCardSuffix}: +{amount:N2} ₴";

                var senderTransaction = TransactionFactory.Create(
                    TransactionType.Transfer,
                    senderAccountId,
                    amount,
                    senderDescription
                );

                var recipientTransaction = TransactionFactory.Create(
                    TransactionType.Transfer,
                    recipient.Id,
                    amount,
                    recipientDescription
                );

                _context.Transactions.Add(senderTransaction);
                _context.Transactions.Add(recipientTransaction);

                await _context.SaveChangesAsync();

                if (dbTransaction != null)
                {
                    await dbTransaction.CommitAsync();
                }

                if (_notifier != null)
                {
                    await _notifier.NotifyAsync(senderTransaction);
                    await _notifier.NotifyAsync(recipientTransaction);
                }
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
    }
}
