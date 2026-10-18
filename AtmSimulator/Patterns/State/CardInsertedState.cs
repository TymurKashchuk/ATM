namespace AtmSimulator.Patterns.State
{
    /// <summary>
    /// Стан банкомата: картка вставлена, очікується введення та верифікація PIN-коду.
    /// </summary>
    public class CardInsertedState : IAtmState
    {
        public string CardNumber { get; }

        public CardInsertedState(string cardNumber)
        {
            CardNumber = cardNumber;
        }

        public string Name => "Картка вставлена (очікування PIN)";

        public bool CanInsertCard() => false;

        public bool CanEnterPin() => true;

        public bool CanPerformTransaction() => false;

        public bool CanEjectCard() => true;
    }
}
