using AtmSimulator.Data;
using AtmSimulator.Models;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Services
{
    public class AuthService
    {
        private readonly AppDbContext _context;
        private const int MaxPinAttempts = 3;

        public AuthService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Card?> FindCardAsync(string cardNumber) {
            return await _context.Cards
                    .Include(c => c.Account)
                    .FirstOrDefaultAsync(c => c.CardNumber == cardNumber);
        }

        public async Task<bool> ValidatePinAsync(Card card,string pin) {
            if (card.IsBlocked) return false;

            if (card.PinHash != pin)
            {
                card.FailedPinAttempts++;
                if (card.FailedPinAttempts >= MaxPinAttempts)
                    card.IsBlocked = true;

                await _context.SaveChangesAsync();
                return false;
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
