using AtmSimulator.Data;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Services
{
    public class PinService
    {
        private readonly AppDbContext _context;

        public PinService(AppDbContext context)
        {
            _context = context;
        }

        public async Task ChangePinAsync(int accountId, string currentPin, string newPin, string confirmPin)
        {
            if (newPin != confirmPin)
                throw new InvalidOperationException("Новий PIN та підтвердження не збігаються");

            if (newPin.Length != 4 || !newPin.All(char.IsDigit))
                throw new InvalidOperationException("PIN має містити рівно 4 цифри");

            if (currentPin == newPin)
                throw new InvalidOperationException("Новий PIN має відрізнятись від поточного");

            var card = await _context.Cards
                .FirstOrDefaultAsync(c => c.AccountId == accountId)
                ?? throw new InvalidOperationException("Картку не знайдено");

            if (card.PinHash != currentPin)
                throw new InvalidOperationException("Поточний PIN невірний");

            card.PinHash = newPin;
            await _context.SaveChangesAsync();
        }
    }
}
