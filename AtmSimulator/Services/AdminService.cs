using AtmSimulator.Data;
using AtmSimulator.Models;
using AtmSimulator.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AtmSimulator.Services
{
    public class AdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<AdminAccountItemViewModel>> GetAccountsAsync(string? search = null)
        {
            var query = _context.Accounts
                .Include(a => a.Card)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(a =>
                    a.OwnerName.Contains(search) ||
                    a.Card!.CardNumber.Contains(search));

            return await query.Select(a => new AdminAccountItemViewModel
            {
                Id = a.Id,
                OwnerName = a.OwnerName,
                CardNumber = a.Card!.CardNumber,
                Balance = a.Balance,
                IsBlocked = a.Card.IsBlocked
            }).ToListAsync();
        }

        public async Task ToggleBlockAsync(int accountId)
        {
            var card = await _context.Cards
                .FirstOrDefaultAsync(c => c.AccountId == accountId);

            if (card == null) throw new InvalidOperationException("Картку не знайдено");

            card.IsBlocked = !card.IsBlocked;
            await _context.SaveChangesAsync();
        }

        public async Task CreateAccountAsync(CreateAccountViewModel model)
        {
            var existingCard = await _context.Cards
                .FirstOrDefaultAsync(c => c.CardNumber == model.CardNumber);

            if (existingCard != null)
                throw new InvalidOperationException("Картка з таким номером вже існує");

            var account = new Account
            {
                OwnerName = model.OwnerName,
                Balance = model.InitialBalance
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var card = new Card
            {
                CardNumber = model.CardNumber,
                PinHash = model.Pin,
                AccountId = account.Id,
                IsBlocked = false
            };

            _context.Cards.Add(card);
            await _context.SaveChangesAsync();
        }
    }
}
