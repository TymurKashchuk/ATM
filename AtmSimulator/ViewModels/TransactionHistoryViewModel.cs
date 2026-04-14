using AtmSimulator.Models;

namespace AtmSimulator.ViewModels
{
    public class TransactionHistoryViewModel
    {
        public List<Transaction> Transactions { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
    }
}
