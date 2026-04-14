using AtmSimulator.Models;

namespace AtmSimulator.Patterns.Observer
{
    public class TransactionNotifier
    {
        private readonly List<ITransactionObserver> _observers = new();

        public void Subscribe(ITransactionObserver observer)
        {
            _observers.Add(observer);
        }

        public async Task NotifyAsync(Transaction transaction)
        {
            foreach (var observer in _observers)
                await observer.OnTransactionAsync(transaction);
        }
    }
}
