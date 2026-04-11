namespace AtmSimulator.Patterns.Strategy
{
    public interface ICashDispenserStrategy
    {
        Dictionary<int, int> Calculate(decimal amount, Dictionary<int, int> availableCash);
    }
}
