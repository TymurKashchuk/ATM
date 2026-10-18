namespace AtmSimulator.Patterns.State
{
    /// <summary>
    /// Стан банкомата: успішна аутентифікація користувача, доступні банківські транзакції.
    /// </summary>
    public class AuthenticatedState : IAtmState
    {
        public string CardNumber { get; }

        public AuthenticatedState(string cardNumber)
        {
            CardNumber = cardNumber;
        }

        public string Name => "Аутентифіковано (активна сесія)";

        public bool CanInsertCard() => false;

        public bool CanEnterPin() => false;

        public bool CanPerformTransaction() => true;

        public bool CanEjectCard() => true;
    }
}
