using AtmSimulator.Data;
using AtmSimulator.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Services
{
    public class PinService
    {
        private readonly AppDbContext _context;
        private readonly IPinHasher _pinHasher;

        public PinService(AppDbContext context, IPinHasher pinHasher)
        {
            _context = context;
            _pinHasher = pinHasher;
        }

        public PinService(AppDbContext context) : this(context, new BCryptPinHasher())
        {
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

            if (!_pinHasher.Verify(currentPin, card.PinHash))
                throw new InvalidOperationException("Поточний PIN невірний");

            card.PinHash = _pinHasher.Hash(newPin);
            await _context.SaveChangesAsync();
        }
    }
}
