namespace AtmSimulator.ViewModels
{
    public class AdminViewModels
    {
        public List<AdminAccountItemViewModel> Accounts { get; set; } = new();
        public string? SearchQuery { get; set; }
    }

    public class AdminAccountItemViewModel
    {
        public int Id { get; set; }
        public string OwnerName { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool IsBlocked { get; set; }
    }

    public class CreateAccountViewModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Введіть ім'я власника")]
        public string OwnerName { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Введіть номер картки")]
        [System.ComponentModel.DataAnnotations.StringLength(16, MinimumLength = 16, ErrorMessage = "Номер картки має бути 16 цифр")]
        public string CardNumber { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Введіть PIN")]
        [System.ComponentModel.DataAnnotations.StringLength(4, MinimumLength = 4, ErrorMessage = "PIN має бути 4 цифри")]
        public string Pin { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Range(0, double.MaxValue, ErrorMessage = "Баланс не може бути від'ємним")]
        public decimal InitialBalance { get; set; }
    }
}
