namespace AtmSimulator.Models
{
    public class CashStorage
    {
        public int Id { get; set; }
        public int Denomination { get; set; }
        public int Count { get; set; }

        public decimal TotalValue => Denomination * Count;
    }
}
