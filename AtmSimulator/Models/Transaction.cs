namespace AtmSimulator.Models
{
    public enum TransactionType
    {
        Withdrawal,
        Deposit,
        Transfer
    }
    public class Transaction
    {
        public int Id { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? Description { get; set; }

        public int AccountId { get; set; }
        public Account? Account { get; set; }
    }
}
