using System.ComponentModel.DataAnnotations;

namespace AtmSimulator.ViewModels
{
    public class TransferViewModel
    {
        [Required(ErrorMessage = "Введіть номер картки одержувача")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Номер картки має містити 16 цифр")]
        public string RecipientCardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть суму")]
        [Range(1, 50000, ErrorMessage = "Сума має бути від 1 до 50 000")]
        public decimal Amount { get; set; }

        public decimal CurrentBalance { get; set; }
        public string? RecipientName { get; set; }
    }
}
