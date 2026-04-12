using System.ComponentModel.DataAnnotations;

namespace AtmSimulator.ViewModels
{
    public class DepositViewModel
    {
        [Required(ErrorMessage = "Введіть суму")]
        [Range(20, 100000, ErrorMessage = "Сума має бути від 20 до 100 000")]
        public decimal Amount { get; set; }

        public decimal CurrentBalance { get; set; }
    }
}
