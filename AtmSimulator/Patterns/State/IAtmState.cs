namespace AtmSimulator.Patterns.State
{
    /// <summary>
    /// Інтерфейс стану банкомата (GoF State Pattern).
    /// Інкапсулює поведінку, що залежить від поточного стану сесії банкомата.
    /// </summary>
    public interface IAtmState
    {
        string Name { get; }
        bool CanInsertCard();
        bool CanEnterPin();
        bool CanPerformTransaction();
        bool CanEjectCard();
    }
}
