using AtmSimulator.Models;

namespace AtmSimulator.ViewModels
{
    public class ReceiptViewModel
    {
        public int TransactionId { get; set; }
        public TransactionType TransactionType { get; set; }
        public string CardNumberMasked { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal RemainingBalance { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TerminalId { get; set; } = "ATM-UA-042";
        public string Status { get; set; } = "УСПІШНО / SUCCESS";

        public string FormattedTransactionType => TransactionType switch
        {
            TransactionType.Withdrawal => "Зняття готівки (Cash Withdrawal)",
            TransactionType.Deposit => "Поповнення рахунку (Cash Deposit)",
            TransactionType.Transfer => "Переказ коштів (Funds Transfer)",
            _ => TransactionType.ToString()
        };
    }
}
