using System.ComponentModel.DataAnnotations;

namespace AtmSimulator.ViewModels
{
    public class CardViewModel
    {
        private string _cardNumber = string.Empty;

        [Required(ErrorMessage = "Введіть номер картки")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Номер картки має бути 16 цифр")]
        public string CardNumber
        {
            get => _cardNumber;
            set => _cardNumber = value?.Replace(" ", "").Trim() ?? string.Empty;
        }
    }

    public class PinViewModel {
        [Required(ErrorMessage = "Введіть PIN")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN має бути 4 цифри")]
        public string Pin { get; set; } = string.Empty;
    }
}
