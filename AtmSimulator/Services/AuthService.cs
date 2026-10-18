using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private readonly IPinHasher _pinHasher;
        private const int MaxPinAttempts = 3;

        public AuthService(AppDbContext context, IPinHasher pinHasher)
        {
            _context = context;
            _pinHasher = pinHasher;
        }

        public AuthService(AppDbContext context) : this(context, new BCryptPinHasher())
        {
        }

        public async Task<Card?> FindCardAsync(string cardNumber) {
            return await _context.Cards
                    .Include(c => c.Account)
                    .FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
        }

        public async Task<bool> ValidatePinAsync(Card card, string pin) {
            if (card.IsBlocked || card.IsExpired) return false;

            if (!_pinHasher.Verify(pin, card.PinHash))
            {
                card.FailedPinAttempts++;
                if (card.FailedPinAttempts >= MaxPinAttempts)
                    card.IsBlocked = true;

                await _context.SaveChangesAsync();
                return false;
            }

            // Якщо PIN був у відкритому вигляді (legacy), прозоро оновлюємо його на BCrypt хеш
            if (_pinHasher.NeedsRehash(card.PinHash))
            {
                card.PinHash = _pinHasher.Hash(pin);
            }

            card.FailedPinAttempts = 0;
            await _context.SaveChangesAsync();
            return true;
        }

        public int GetRemainingAttempts(Card card) {
            return MaxPinAttempts - card.FailedPinAttempts;
        }
    }
}
