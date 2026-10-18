namespace AtmSimulator.Patterns.State
{
    /// <summary>
    /// Контекст банкомата, який зберігає поточний стан та делегує операції відповідно до State Pattern.
    /// </summary>
    public class AtmContext
    {
        public IAtmState CurrentState { get; private set; }

        public AtmContext()
        {
            CurrentState = new NoCardState();
        }

        public AtmContext(IAtmState initialState)
        {
            CurrentState = initialState;
        }

        public void TransitionTo(IAtmState state)
        {
            CurrentState = state;
        }

        public void InsertCard(string cardNumber)
        {
            if (!CurrentState.CanInsertCard())
                throw new InvalidOperationException($"Неможливо вставити картку: банкомат перебуває у стані '{CurrentState.Name}'");

            if (string.IsNullOrWhiteSpace(cardNumber))
                throw new ArgumentException("Номер картки не може бути порожнім", nameof(cardNumber));

            TransitionTo(new CardInsertedState(cardNumber));
        }

        public void Authenticate()
        {
            if (!CurrentState.CanEnterPin())
                throw new InvalidOperationException($"Неможливо пройти аутентифікацію: банкомат перебуває у стані '{CurrentState.Name}'");

            if (CurrentState is CardInsertedState cardInserted)
            {
                TransitionTo(new AuthenticatedState(cardInserted.CardNumber));
            }
        }

        public void EjectCard()
        {
            if (!CurrentState.CanEjectCard())
                throw new InvalidOperationException($"Неможливо витягти картку: банкомат перебуває у стані '{CurrentState.Name}'");

            TransitionTo(new NoCardState());
        }

        public void EnsureCanTransact()
        {
            if (!CurrentState.CanPerformTransaction())
                throw new InvalidOperationException($"Неможливо виконати операцію: банкомат перебуває у стані '{CurrentState.Name}'");
        }
    }
}
