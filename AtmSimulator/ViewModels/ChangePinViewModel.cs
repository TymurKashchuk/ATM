using System.ComponentModel.DataAnnotations;

namespace AtmSimulator.ViewModels
{
    public class ChangePinViewModel
    {
        [Required(ErrorMessage = "Введіть поточний PIN")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN має бути 4 цифри")]
        public string CurrentPin { get; set; } = string.Empty;

        [Required(ErrorMessage = "Введіть новий PIN")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN має бути 4 цифри")]
        public string NewPin { get; set; } = string.Empty;

        [Required(ErrorMessage = "Підтвердіть новий PIN")]
        [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN має бути 4 цифри")]
        public string ConfirmPin { get; set; } = string.Empty;
    }
}
