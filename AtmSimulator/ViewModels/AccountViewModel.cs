namespace AtmSimulator.ViewModels
{
    public class AccountViewModel
    {
        public string OwnerName { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
    }
}
