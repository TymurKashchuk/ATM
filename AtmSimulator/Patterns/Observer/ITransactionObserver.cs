using AtmSimulator.Models;

namespace AtmSimulator.Patterns.Observer
{
    public interface ITransactionObserver
    {
        Task OnTransactionAsync(Transaction transaction);
    }
}
