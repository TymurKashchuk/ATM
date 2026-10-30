using System.ComponentModel.DataAnnotations;
using AtmSimulator.Services;

namespace AtmSimulator.ViewModels
{
    public class TransferViewModel
    {
        private string _recipientCardNumber = string.Empty;

        [Required(ErrorMessage = "Введіть номер картки одержувача")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Номер картки має містити 16 цифр")]
        public string RecipientCardNumber
        {
            get => _recipientCardNumber;
            set => _recipientCardNumber = value?.Replace(" ", "").Trim() ?? string.Empty;
        }

        [Required(ErrorMessage = "Введіть суму")]
        [Range(1, 50000, ErrorMessage = "Сума має бути від 1 до 50 000 ₴")]
        public decimal Amount { get; set; }

        public decimal CurrentBalance { get; set; }
        public string? RecipientName { get; set; }
        public string SenderCardNumber { get; set; } = string.Empty;
        public decimal ExternalFeePercent { get; set; } = TransferService.ExternalTransferFeePercent;
    }
}
