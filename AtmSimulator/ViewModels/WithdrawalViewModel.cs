using System.ComponentModel.DataAnnotations;
using AtmSimulator.Services;

namespace AtmSimulator.ViewModels
{
    public class WithdrawalViewModel
    {
        [Required(ErrorMessage = "Введіть суму")]
        [Range(20, 20000, ErrorMessage = "Сума має бути від 20 до 20 000 ₴")]
        public decimal Amount { get; set; }

        public decimal CurrentBalance { get; set; }
        public decimal RemainingDailyLimit { get; set; }
        public decimal DailyLimit { get; set; } = WithdrawalService.DailyWithdrawalLimit;
        public decimal FeePercent { get; set; } = WithdrawalService.WithdrawalFeePercent;
        public Dictionary<int, int>? DispensedCash { get; set; }
    }
}
