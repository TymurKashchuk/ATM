using System.ComponentModel.DataAnnotations;

namespace AtmSimulator.ViewModels
{
    public class WithdrawalViewModel
    {
        [Required(ErrorMessage = "Введіть суму")]
        [Range(20, 50000, ErrorMessage = "Сума має бути від 20 до 50 000")]
        public decimal Amount { get; set; }

        public decimal CurrentBalance { get; set; }
        public Dictionary<int, int>? DispensedCash { get; set; }
    }
}
