using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace AtmSimulator.Services
{
    public class ReceiptService
    {
        private readonly AppDbContext _context;

        public ReceiptService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ReceiptViewModel> GenerateReceiptAsync(int transactionId)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Account)
                .ThenInclude(a => a!.Card)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null)
                throw new InvalidOperationException($"Транзакцію #{transactionId} не знайдено");

            var account = transaction.Account
                ?? throw new InvalidOperationException("Рахунок, пов'язаний з транзакцією, не знайдено");

            var card = account.Card;
            var cardNumber = card?.CardNumber ?? "****************";
            var maskedCard = cardNumber.Length >= 4
                ? $"**** **** **** {cardNumber[^4..]}"
                : "****";

            decimal fee = 0m;
            if (!string.IsNullOrEmpty(transaction.Description))
            {
                var match = Regex.Match(transaction.Description, @"Комісія.*:\s*([\d\.,]+)\s*₴");
                if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(',', '.'),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var parsedFee))
                {
                    fee = parsedFee;
                }
            }

            var totalDebit = transaction.Type == TransactionType.Deposit
                ? transaction.Amount
                : transaction.Amount + fee;

            return new ReceiptViewModel
            {
                TransactionId = transaction.Id,
                TransactionType = transaction.Type,
                CardNumberMasked = maskedCard,
                OwnerName = account.OwnerName,
                DateTime = transaction.CreatedAt,
                Amount = transaction.Amount,
                Fee = fee,
                TotalDebit = totalDebit,
                RemainingBalance = account.Balance,
                Description = transaction.Description ?? string.Empty,
                TerminalId = "ATM-UA-042",
                Status = "УСПІШНО / SUCCESS"
            };
        }
    }
}
