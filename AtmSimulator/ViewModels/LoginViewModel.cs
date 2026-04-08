using System.ComponentModel.DataAnnotations;

namespace AtmSimulator.ViewModels
{
    public class CardViewModel
    {
        [Required(ErrorMessage = "Введіть нрмер картки")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Номер картки має бути 16 цифр")]
        public string CardNumber { get; set; } = string.Empty;
    }

    public class PinViewModel {
        [Required(ErrorMessage = "Введіть PIN")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN має бути 4 цифри")]
        public string Pin { get; set; } = string.Empty;
    }
}
