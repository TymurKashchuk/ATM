namespace AtmSimulator.Patterns.State
{
    /// <summary>
    /// Стан банкомата: картка відсутня (очікування вставлення картки).
    /// </summary>
    public class NoCardState : IAtmState
    {
        public string Name => "Очікування картки";

        public bool CanInsertCard() => true;

        public bool CanEnterPin() => false;

        public bool CanPerformTransaction() => false;

        public bool CanEjectCard() => false;
    }
}
