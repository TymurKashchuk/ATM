using System.ComponentModel.DataAnnotations;

namespace AtmSimulator.ViewModels
{
    public class BanknoteDepositItem
    {
        public int Denomination { get; set; }

        [Range(0, 500, ErrorMessage = "Кількість купюр має бути від 0 до 500")]
        public int Count { get; set; }
    }

    public class DepositViewModel
    {
        public decimal CurrentBalance { get; set; }
        public List<BanknoteDepositItem> Banknotes { get; set; } = new();
        public decimal TotalAmount => Banknotes.Sum(b => b.Denomination * b.Count);
    }
}
