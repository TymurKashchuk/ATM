namespace AtmSimulator.Patterns.Strategy
{
    public class GreedyCashDispenserStrategy : ICashDispenserStrategy
    {
        private static readonly int[] Denominations = { 1000,500, 200, 100, 50, 20 };
        public Dictionary<int, int> Calculate(decimal amount, Dictionary<int, int> availableCash) {
            var result = new Dictionary<int, int>();
            var remaining = (int)amount;

            foreach (var denomination in Denominations) {
                if (remaining <= 0) break;
                if (!availableCash.ContainsKey(denomination)) continue;

                var needed = remaining / denomination;
                var available = availableCash[denomination];
                var toDispense = Math.Min(needed,available);

                if (toDispense > 0) {
                    result[denomination] = toDispense;
                    remaining -= toDispense * denomination;
                }
            }

            if (remaining > 0)
                throw new InvalidOperationException("Cannot dispense exact amount with available denominations");

            return result;
        }
    }
}
