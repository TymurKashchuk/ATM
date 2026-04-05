namespace AtmSimulator.Models
{
    public class Card
    {
        public int Id { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string PinHash { get; set; } = string.Empty;
        public bool IsBlocked { get; set; } = false;
        public int FailedPinAttempts { get; set; } = 0;
        public DateTime ExpiryDate { get; set; }

        public int AccountId { get; set; }
        public Account? Account { get; set; }
    }
}
