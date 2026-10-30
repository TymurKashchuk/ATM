namespace AtmSimulator.ViewModels
{
    public class CashCassetteItemViewModel
    {
        public int Id { get; set; }
        public int Denomination { get; set; }
        public int Count { get; set; }
        public decimal TotalValue => (decimal)Denomination * Count;
        public bool IsLow => Count < 10;
    }

    public class CashStorageViewModel
    {
        public List<CashCassetteItemViewModel> Cassettes { get; set; } = new();
        public decimal TotalCashAmount => Cassettes.Sum(c => c.TotalValue);
        public int TotalBanknotesCount => Cassettes.Sum(c => c.Count);
    }
}
